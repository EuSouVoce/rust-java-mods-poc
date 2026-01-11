# Carbon Setup Guide

This guide explains how to set up the Rust-Java Modding Framework with Carbon on a Facepunch Rust server.

## Prerequisites

- A Facepunch Rust dedicated server (Windows or Linux)
- .NET Framework 4.7.2+ (Windows) or Mono (Linux)
- Java 11+ JDK
- Carbon framework installed on your Rust server

## Step 1: Install Carbon Framework

1. Download Carbon from the official website: https://carbonmod.gg/
2. Follow the Carbon installation instructions for your platform:
   - **Windows**: Extract Carbon files to your Rust server directory
   - **Linux**: Extract and run the installation script

3. Verify Carbon is installed:
   ```bash
   # Check for carbon folder in server directory
   ls -la carbon/
   ```

4. Start your Rust server once to ensure Carbon loads properly

## Step 2: Build the RustJavaBridge Plugin

### On Windows:

```powershell
cd csharp-bridge
dotnet build --configuration Release
```

### On Linux:

```bash
cd csharp-bridge
dotnet build --configuration Release
```

The compiled plugin will be at:
```
csharp-bridge/bin/Release/net472/RustJavaBridge.dll
```

## Step 3: Deploy the Carbon Plugin

1. Copy the compiled DLL to your Rust server:
   ```bash
   # Windows
   copy csharp-bridge\bin\Release\net472\RustJavaBridge.dll YourRustServer\carbon\plugins\

   # Linux
   cp csharp-bridge/bin/Release/net472/RustJavaBridge.dll /path/to/rust-server/carbon/plugins/
   ```

2. Restart your Rust server or use Carbon's hot-reload command:
   ```
   c.reload RustJavaBridge
   ```

3. Check the console for successful initialization:
   ```
   ✓ Rust-Java Bridge initialized successfully
     IPC Endpoint: \\.\pipe\rust-java-mods  (Windows)
     IPC Endpoint: /tmp/rust-java-mods.sock (Linux)
     Using Carbon Framework
   ```

## Step 4: Build the Java API

```bash
cd java-plugin
mvn clean package
```

The compiled JAR will be at:
```
java-plugin/target/rust-java-mods-api-0.1.0.jar
```

## Step 5: Create Your First Java Mod

1. Create a new Java project
2. Add the API as a dependency:
   ```xml
   <dependency>
       <groupId>com.rustjavamods</groupId>
       <artifactId>rust-java-mods-api</artifactId>
       <version>0.1.0</version>
       <scope>system</scope>
       <systemPath>${project.basedir}/lib/rust-java-mods-api-0.1.0.jar</systemPath>
   </dependency>
   ```

3. Create your mod:
   ```java
   import com.rustjavamods.RustModAPI;
   import com.rustjavamods.game.*;

   public class MyFirstMod {
       public static void main(String[] args) {
           RustModAPI api = RustModAPI.getInstance();
           api.initialize();

           RustGameEvents.onPlayerConnected((playerId, playerName, steamId) -> {
               System.out.println("Welcome, " + playerName + "!");
           });

           System.out.println("Mod loaded!");
       }
   }
   ```

4. Run your mod:
   ```bash
   java -cp rust-java-mods-api-0.1.0.jar:MyMod.jar MyFirstMod
   ```

## Step 6: Test the Integration

1. Start your Rust server with Carbon and the RustJavaBridge plugin
2. Start your Java mod application
3. Connect to your Rust server
4. Watch the console output on both sides!

**Expected output (Carbon console):**
```
[RustJavaBridge] Event: player_connected
```

**Expected output (Java console):**
```
Welcome, YourPlayerName!
```

## Troubleshooting

### Plugin doesn't load

**Problem**: Carbon doesn't load RustJavaBridge.dll

**Solutions**:
- Check Carbon logs: `carbon/logs/carbon.log`
- Verify .NET Framework 4.7.2+ is installed
- Ensure the DLL is not blocked (Windows: Right-click → Properties → Unblock)
- Check file permissions on Linux

### IPC Connection Failed

**Problem**: Java mod can't connect to Carbon plugin

**Solutions**:
- Verify the Carbon plugin initialized successfully
- Check that the socket/pipe exists:
  - Windows: Named pipe should be visible
  - Linux: Check `/tmp/rust-java-mods.sock` exists
- Ensure no firewall is blocking local connections
- Try running Java mod as administrator/root

### Hooks not working

**Problem**: Java hooks registered but not executing

**Solutions**:
- Check that both Carbon plugin and Java mod are running
- Verify IPC connection is established
- Enable verbose logging in both components
- Check Carbon console for hook messages

## Advanced Configuration

### Custom IPC Endpoint

To change the IPC endpoint, edit `RustJavaBridge.cs`:

```csharp
// Change this line in IpcServer constructor
_endpoint = _isWindows 
    ? "my-custom-pipe-name" 
    : "/tmp/my-custom-socket.sock";
```

### Performance Tuning

For high-traffic servers, consider:
- Filtering entity spawn events (already done in plugin)
- Batching events on Java side
- Using separate threads for heavy processing

## Carbon Plugin Configuration

Currently no configuration file is needed. Future versions may include:
```json
{
  "ipc_endpoint": "rust-java-mods",
  "enable_entity_events": false,
  "log_level": "info"
}
```

## Updating

To update the plugin:

1. Pull latest code
2. Rebuild the Carbon plugin
3. Stop your Rust server
4. Replace the DLL in `carbon/plugins/`
5. Restart the server

Or use Carbon's hot-reload:
```
c.reload RustJavaBridge
```

## Getting Help

- Check `carbon/logs/carbon.log` for errors
- Enable verbose logging in both C# and Java
- Review the ARCHITECTURE.md for technical details
- Test IPC connection manually (see ARCHITECTURE.md)

## Next Steps

- Explore more hooks in `RustGameHooks.java`
- Check out example mods in `java-plugin/examples/`
- Read the full API documentation
- Join the community (if available)
