# DSPM: DS Process Manager
by Cannon - 15 Feb 2010

## Overview

This program starts and restarts FLServer if it crashes. The program will restart flserver if too much memory is used, the server takes too long to start up or for automatic daily maintenance. The program also provides chat logging, an online player list and limited network monitoring. The program operates best with flhook sockets as these are used to determine to if the server is operating correctly. 

## Installation

- You will need .NET 4.8 client libraries installed. This is installed on an updated Windows 10 or Server 2022 by default. If you don't have these download and install them from the Microsoft web site.
- Configure your FLHook to listen for connections. Your FLHook.json "socket" section should look something like:

```
"socket": {
	"activated": true,
	"ePort": 1921,
	"eWPort": 1922,
	"encryptionKey": "SomeRandomKey000",
	"passRightsMap": {
		"somepasswordhere": "superadmin"
	},
	"port": 1919,
	"wPort": 1920
}
```
- Start DSProcessManager. Click on the "Settings" button.
- Under EXE options, set the path to your FLServer.exe by clicking the browse button.
- Under FLSettings, enter the "port" and password from the "passRightsMap".
- If FLServer is not already running, the program will automatically start FLServer.
- DSPM will attempt to connect to flhook and you will see an entry like "18/11/2009 12:09:56 a.m.:Server is running: connected to flhook". If you do not see this then the program will automatically restart the server after 15 minutes and you likely have a configuration problem with either FLHook, the port or password.
- If you minimise the process manager then it will minimise to the task bar notification area. If an entry is added to the log, i.e. the server is restarted then the window is automatically opened.

## Automatic scripts used for backups

This program can run two scripts at a specified time each day. This can be used to implement an automatic backups. It can also run a command when FLServer is restarted.

## Source

This is free software and the source code is included with this package. You may do what ever you like with it but if you use it or parts of it in another project I would appreciate being notified and/or mentioned somewhere. If you fix or improve the software, I'll happily include your changes in the official release. 

The most up to date version should be available from https://github.com/TheStarport/DS-Process-Manager/

Report bugs by raising an issue on this repo.

## Change History

1.6: First public release
1.7: Fixed bug that caused the program to use a lot of CPU when the connection to FLHook breaks and re-established.
2.0: Rewrote the socket stuff so that it actually works.
2.2: Background threads terminate properly now.
2024: Ported to GitHub due to the Forge being down.
