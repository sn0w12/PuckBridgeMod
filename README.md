# PuckBridgeMod

A bridge mod for Puck that enables real-time communication between the game server and external applications via TCP connection.

## Overview

PuckBridgeMod creates a TCP bridge that allows external applications to receive game events and send commands to the Puck game server. The mod captures game state changes, player events, and goals, while providing a command interface for server management.

## Features

-   Real-time game state monitoring
-   Goal scoring event tracking
-   Player spawn/despawn notifications
-   Performance metrics collection
-   Remote command execution
-   Automatic reconnection handling
-   Configurable bridge port

## Installation

1. Copy the compiled mod files to your Puck mods directory
2. Launch the game server with the `-bridgeport` argument to specify the TCP port (default: 9000)

```bash
# Windows
PuckServer.exe -bridgeport 9001

# Linux
start_server.sh -bridgeport 9001
```

## Architecture

### Core Components

-   **BridgeClient**: Manages TCP connection and message handling
-   **BridgeMessage**: Standardized message format for communication
-   **CommandHandler**: Processes incoming commands from external clients
-   **MessageProcessor**: Handles queued messages and game actions

### Message Types

#### Outgoing Messages (Server to Client)

**Game State Updates**

```json
{
    "role": "server",
    "type": "game_state",
    "payload": {
        "category": "game_state",
        "phase": "Playing",
        "time": 300.5,
        "period": 1,
        "scores": {
            "blue": 2,
            "red": 1
        }
    }
}
```

**Goal Events**

```json
{
    "role": "server",
    "type": "event",
    "payload": {
        "category": "goal_scored",
        "team": "Blue",
        "scores": {
            "blue": 3,
            "red": 1
        },
        "players": {
            "goal": {
                "username": "PlayerName",
                "clientId": 12345
            },
            "assist": null,
            "secondAssist": null
        },
        "puck": {
            "speed": 15.2,
            "shotSpeed": 18.7
        }
    }
}
```

**Performance Metrics**

```json
{
    "role": "server",
    "type": "status",
    "payload": {
        "category": "performance",
        "fps": 60,
        "memory": 1024,
        "playerCount": 8
    }
}
```

#### Incoming Messages (Client to Server)

**System Commands**

```json
{
    "role": "client",
    "type": "control",
    "payload": {
        "command": "system_message",
        "message": "Server maintenance in 5 minutes"
    }
}
```

**Game Commands**

```json
{
    "role": "client",
    "type": "control",
    "payload": {
        "command": "restart_game"
    }
}
```

**Player Management**

```json
{
    "role": "client",
    "type": "control",
    "payload": {
        "command": "kick_player",
        "playerId": "12345",
        "reason": "Rule violation"
    }
}
```

## Available Commands

-   `system_message` - Send a system message to all players
-   `restart_game` - Restart the current game
-   `kick_player` - Kick a specific player from the server

## Configuration

### Command Line Arguments

-   `-bridgeport <port>` - Set the TCP bridge port (default: 9000)

### Connection Settings

-   **Host**: 127.0.0.1 (localhost only)
-   **Protocol**: TCP
-   **Message Format**: JSON with newline delimiter
-   **Reconnection**: Automatic with exponential backoff (1s to 30s)

## Development

### Building

This mod requires:

-   .NET Framework compatible with the target Puck version
-   Newtonsoft.Json for JSON serialization

### Extending Commands

To add new commands, implement the `ICommand` interface and register it in the `CommandHandler`:

```csharp
public class CustomCommand : ICommand
{
    public string CommandName => "custom_command";

    public void Execute(object payload)
    {
        // Command implementation
    }
}

// Register in CommandHandler constructor
RegisterCommand(new CustomCommand());
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
