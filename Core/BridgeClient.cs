using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace PuckBridgeMod.Core {
    public class BridgeClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _listenThread;
        private Thread _reconnectThread;
        private volatile bool _running;
        private volatile bool _connected;

        private readonly string _host;
        private readonly int _port;
        private readonly int _baseRetryDelay = 1000; // 1 second
        private readonly int _maxRetryDelay = 30000; // 30 seconds

        public BridgeClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Start()
        {
            _running = true;
            _reconnectThread = new Thread(ReconnectLoop) { IsBackground = true };
            _reconnectThread.Start();
        }

        public void Stop()
        {
            Util.Logger.Info("Stopping bridge client...");
            _running = false;
            _connected = false;

            // Close connections to interrupt blocking calls
            try
            {
                _stream?.Close();
            }
            catch (Exception ex)
            {
                Util.Logger.Debug($"Error closing stream: {ex.Message}");
            }

            try
            {
                _client?.Close();
            }
            catch (Exception ex)
            {
                Util.Logger.Debug($"Error closing client: {ex.Message}");
            }

            // Give threads a short time to exit gracefully, then abort if needed
            bool reconnectStopped = _reconnectThread?.Join(2000) ?? true;
            bool listenStopped = _listenThread?.Join(2000) ?? true;

            if (!reconnectStopped)
            {
                Util.Logger.Warning("Reconnect thread did not stop gracefully, aborting");
                try
                {
                    _reconnectThread?.Abort();
                }
                catch (Exception ex)
                {
                    Util.Logger.Debug($"Error aborting reconnect thread: {ex.Message}");
                }
            }

            if (!listenStopped)
            {
                Util.Logger.Warning("Listen thread did not stop gracefully, aborting");
                try
                {
                    _listenThread?.Abort();
                }
                catch (Exception ex)
                {
                    Util.Logger.Debug($"Error aborting listen thread: {ex.Message}");
                }
            }

            Util.Logger.Info("Bridge client stopped");
        }

        private void ReconnectLoop()
        {
            int retryDelay = _baseRetryDelay;

            while (_running)
            {
                try
                {
                    if (!_connected && _running)
                    {
                        try
                        {
                            _client?.Close();
                        }
                        catch { }

                        _client = new TcpClient();

                        // Set a connection timeout to avoid hanging
                        _client.ReceiveTimeout = 5000;
                        _client.SendTimeout = 5000;

                        _client.Connect(_host, _port);
                        _stream = _client.GetStream();
                        _connected = true;

                        _listenThread = new Thread(Listen) { IsBackground = true };
                        _listenThread.Start();

                        Util.Logger.Info($"Connected to {_host}:{_port}");
                        retryDelay = _baseRetryDelay; // Reset delay on successful connection
                    }

                    // Use shorter sleep intervals to check _running more frequently
                    for (int i = 0; i < 50 && _running; i++)
                    {
                        Thread.Sleep(100); // Check every 100ms for 5 seconds total
                    }
                }
                catch (ThreadAbortException)
                {
                    Util.Logger.Debug("Reconnect thread aborted");
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        Util.Logger.Error($"Connection failed, retrying in {retryDelay}ms", ex);
                    }
                    _connected = false;

                    // Sleep in smaller chunks to respond to stop requests faster
                    int elapsed = 0;
                    while (elapsed < retryDelay && _running)
                    {
                        Thread.Sleep(Math.Min(100, retryDelay - elapsed));
                        elapsed += 100;
                    }

                    retryDelay = Math.Min(retryDelay * 2, _maxRetryDelay); // Exponential backoff
                }
            }
            Util.Logger.Debug("Reconnect loop exited");
        }

        private void Listen()
        {
            byte[] buffer = new byte[4096];

            while (_running && _connected)
            {
                try
                {
                    // Check if stream is still available
                    if (_stream == null || !_stream.CanRead)
                    {
                        _connected = false;
                        break;
                    }

                    // Use a timeout to avoid hanging on Read
                    if (_client.Available > 0 || _stream.DataAvailable)
                    {
                        int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            var message = JsonConvert.DeserializeObject<BridgeMessage>(json);
                            Util.Logger.Debug($"Received: {json}");
                            HandleIncomingMessage(message);
                        }
                        else
                        {
                            // Connection closed by remote host
                            _connected = false;
                            break;
                        }
                    }
                    else
                    {
                        // No data available, sleep briefly to avoid busy waiting
                        Thread.Sleep(50);
                    }
                }
                catch (ThreadAbortException)
                {
                    Util.Logger.Debug("Listen thread aborted");
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        Util.Logger.Error("Error in Listen", ex);
                    }
                    _connected = false;
                    break;
                }
            }
            Util.Logger.Debug("Listen loop exited");
        }

        private void HandleIncomingMessage(BridgeMessage message)
        {
            try
            {
                if (message.Role == "client" && message.Type == "control")
                {
                    var payload = message.Payload as Newtonsoft.Json.Linq.JObject;

                    if (payload != null)
                    {
                        string command = payload["command"]?.ToString();
                        if (!string.IsNullOrEmpty(command))
                        {
                            // Use the command handler
                            bool handled = PuckBridgeMod._instance?.CommandHandler?.HandleCommand(command, payload) ?? false;

                            if (!handled)
                            {
                                Util.Logger.Warning($"Unhandled command: {command}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Util.Logger.Error("Error handling incoming message", ex);
            }
        }

        public void SendMessage(BridgeMessage message)
        {
            if (_stream != null && _stream.CanWrite && _connected)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(message);
                    string messageWithDelimiter = json + "\n";
                    byte[] data = Encoding.UTF8.GetBytes(messageWithDelimiter);
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush(); // Ensure data is sent immediately
                    Util.Logger.Debug($"Sent: {json}");
                }
                catch (Exception ex)
                {
                    Util.Logger.Error("Error sending message", ex);
                    _connected = false;
                }
            }
            else
            {
                Util.Logger.Warning("Cannot send message - not connected");
            }
        }

        public void SendPerformanceData(object performanceData)
        {
            var message = new BridgeMessage
            {
                Role = "server",
                Type = "status",
                Payload = performanceData
            };

            SendMessage(message);
        }

        public void SendGameStateData(object gameStateData)
        {
            var message = new BridgeMessage
            {
                Role = "server",
                Type = "game_state",
                Payload = gameStateData
            };

            SendMessage(message);
        }

        public void SendGoalData(object goalData)
        {
            var message = new BridgeMessage
            {
                Role = "server",
                Type = "event",
                Payload = goalData
            };

            SendMessage(message);
        }

        public void SendPlayerData(object playerData)
        {
            var message = new BridgeMessage
            {
                Role = "server",
                Type = "event",
                Payload = playerData
            };

            SendMessage(message);
        }
    }
}