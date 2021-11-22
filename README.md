![RESTbot logo](assets/images/RESTbot-logo.png)

# README File - RESTBot

## Revisions
- Written August 9, 2007 -  Pleiades Consulting, Inc
- Revised June 5, 2010 - Gwyneth Llewelyn (notes on compilation)
- Revised November 21, 2021 - Gwyneth Llewelyn (updates for a lot of things)

# Overview

RESTBot is a C# webserver that uses [RESTful transactions](https://en.wikipedia.org/wiki/Representational_state_transfer) to interact with a
_bot_ in [Second Life](https://secondlife.com) or [OpenSimulator](http://opensimulator.org). This bot (or a collection of bots) is started through a REST command and can be stopped the same way. The software is extremely
modular, and is designed to easily accept plugins that developers can write
(in C#) to add more complex interaction between a web application and a
Second Life Bot. More information about Plugins is on the wiki. (See [LINKS](LINKS.md).)

## Preparing the compilation environment

### Under Windows (not tested)

Probably you only need to get the Community version of [Visual Studio](https://visualstudio.microsoft.com/), which should install everything you need.

### Under macOS:

First, get the free [Microsoft .NET environment](https://dotnet.microsoft.com/download/dotnet/scripts). `libremetaverse` needs at least version 4.8

or you can simply go to the root of this project and run:

```bash
wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --os osx
```

**Note:** This script requires a working installation of `curl`; it is supposed to come by default on macOS, but if doesn't, you might need to install it.

Also, this script is _not_ included in RESTbot, since it's unclear what license has been attached to it by Microsoft (allegedly it's MIT); you'll have to retrieve all Microsoft-related things by yourself.

Also note that Microsoft designed that script to use `bash`. These days, Apple 'decided' that macOS ought to have `zsh` as the default shell. If the script doesn't run on your system, you'll need to get `bash`.

Once that step is finished, you should have a working installation of the whole .NET package under `/Users/<your username>/.dotnet`. You can also deploy it to a different directory, if you wish.

This should be enough to be able to run:

```bash
~/.dotnet/dotnet msbuild RestBot.libremetaverse.build
```

For more performance during compilation, you can run `msbuild` with the `-m` flag, which will use all available CPUs on your system (as opposed to just one).

Install `mono`. To save time (in fixing dependencies...) just use [Homebrew](https://brew.sh):

```bash
brew install mono
```

### Under Linux:

It's basically the same as under macOS. These days, Homebrew also works under Linux, so the instructions would be the same; but possibly you will prefer to run your 'native' package manager, be it `apt` (Debian/Linux) or `yum` (Fedora, CentOS, RedHat) or whatever is fashionable these days. You'll have to check what versions of Mono are available; remember that you'll need a 'developer' edition, and don't forget to double-check that `msbuild` comes as part of the package as well.

`dotnet-install.sh` is a bit better at detecting Linux variants, but if it gets it wrong, you can use the same approach as above; run `./dotnet-install.sh --help` to see what variants are currently supported.

# Legacy Instructions

The instructions below were originally written by an anonymous collaborator at Pleiades, and have been only marginally changed from the original document (written circa 2007). They have merely historical interest, and are kept here mainly because the licensing terms are a bit obscure about what to do with existing _documentation_ (as opposed to code), so you can simply skip to the [Configuration](#configuration) section.

## Compiling - Windows

Compiling RESTBot is very easy on a Windows machine. First, you
need [Visual Studio](https://visualstudio.microsoft.com/) 2007 or later (Express Edition is perfect.) The great thing about this IDE, is that you can download the Express edition for FREE! (See [LINKS](LINKS.md))
We used Visual Studio C# Express, and we will use this IDE for any Windows-based instructions.

To compile under windows, open up VS and open the RESTBOT-VS2005-sln.sln file
located in the project's root directory. This will open up restbot's solution
in the IDE.

In the Solution Explorer, make sure that the `RESTBOT-VS2005-csproj` project
is **bold** (the startup project) — if it is not, right click the project and
select **Set as Startup Project**.

To build, all you have to do is go to **Build -> Build Solution** (or you can
press <key>F6</key>)

On the status bar (the text at the bottom of the window of VS) - you should see
`Build succeeded`, which means that the compilation was complete. You can now
skip to the CONFIGURATION section of this readme file.

_Note:_ the current version was **NOT** tested under Windows. _Caveat utilitor_.

## Compiling - Linux

In order to compile on a Linux machine, you need at least the following
applications installed on your system:

* `nANT`
* Mono 1.2.4 or better (with .NET 2.0 package)
* `gmcs` (known as the `mono-gmcs` package on Debian/Ubuntu)

Once all of that is installed, `cd` to the main directory of your copy of RESTbot
and run `nant` in the command line. A lot of gibberish will run across the
screen. This usually takes about 20 seconds or so... and hopefully you will see
`BUILD SUCCEEDED` when it is done. Yay! Go on to the CONFIGURATION section.

Note: if the compilation fails, try the following. First go to the `./libopenmetaverse` folder. Run `./runprebuild.sh` and do a `nant clean` followed by `nant`. This should compile the `libopenmetaverse` library cleanly. Go back to the root directory and run nant again. This time it should work properly.

### Notes

1. _The above is being revised, as `nant` is, for all purposes, deemed to be a deprecated, legacy tool. We'll be moving to `msbuild` instead._

2. _`libopenmetaverse` seems to have been frozen circa 2018. To get access to the latest and greatest features, we're moving to [`libremetaverse`](https://github.com/cinderblocks/libremetaverse) instead (gwyneth 20211121)._

3. _macOS compilation should be pretty much the same thing; use [Homebrew](https://brew.sh) to install whatever packages you need (gwyneth 20211121)._

*** *** ***

# Configuration

Configuration is simple with our XML compatibility in RESTBot. Configuration
is read at startup of the RESTbot server, so you have to have the configuration
file along with the `restbot.exe` binary. The default configuration file is
`configuration.xml` located in `./restbot-bin`; we've provided a `configuration.xml.sample`, make sure you copy this file to `configuration.xml` and change it to reflect your (real) data.

The configuration file is very flexible... if a configuration element is not defined in the file, it will revert
to its default instead of stopping... so a custom configuration file can either
define every single variable, or only contain one variable definition. If you
want to go ahead and play with a RESTbot server now, you can go ahead and skip
ahead to [Running](#running-windows) (by default, the server listens to `localhost` connections on
port 9080).

You can play around with the settings in `configuration.xml` at any time, you just
have to restart the server in order for any changes to take effect. The options
are fairly self explanatory, but if you need help you can always refer to the
[wiki](https://github.com/GwynethLlewelyn/restbot/wiki) (see [LINKS](LINKS.md)).

# Running - Windows

Running on Windows is very easy. Just navigate to the restbot-bin folder and
then execute the restbot.exe file. This runs in the command line, so you will
be presented with a black console with information/debug text. Errors will
appear in yellow, warnings will appear in read, and informational statements
will appear in white. If you see blue text, that is OK too — it just means one
of our developers was lazy and didn't remove his debug from his code. :)
To stop, you can just exit out of the console and the server will shutdown.

# Running - Linux

Running on Linux is easy, once you get it working the first time. `cd` to the
restbot-bin directory and run `mono restbot.exe` - if you get any problems,
try running `sudo mono restbot.exe` or run the above command in superuser mode.

If you have any more questions, visit the wiki (see (LINKS)[LINKS.md]) where you can search
for a solution or find contact information for professional help.

_Note: At present, it's hard to figure out if Pleiades is still around or not (gwyneth 20211121)._

# Basic Commands

Once RESTBot is running, you can connect to it via HTTP REST commands. For now,
I will use `curl` on Linux to demonstrate basic commands you need to know to get
running with RESTbot.

First, I am running RESTbot on a Ubuntu Linux machine. It uses the default
network settings (`localhost` on port 9080)

A note on how commands are passed to RESTbot for processing. Server commands
require a password, which is set in the security block of the configuration
file. The default password is (yes, I know) `pass`

The following is a list of working server commands:

* `establish_session`: this starts up a RESTBot
* `exit`: this stops a RESTBot

_Note: There are plenty more commands these days that are also fully functional (gwyneth 20211121)._

Server commands are defined in the URL and arguments are passed through POST.
The most important command you will use is `establish_session` so I will first
introduce you to its parameters.

Lets say we have a hypothetical bot account whose name is `Restbot Zaius` and
uses the password `omgrestbot` to login. To log into this bot, we would issue
this command in curl:
```bash
curl http://localhost:9080/establish_session/pass -d first=Restbot -d last=Zaius -d pass=77e854984fd6a73ece3aedab7ee9e21c
```

Some things to note in this command... everything after `-d` is a **post field**. In
reality, _all_ of the post fields are spliced together as a single string
seperated by &'s. Also, the password *must* be `md5`'d. Lastly, notice how the
`establish_session` URL is formatted. http://localhost:9080 is the RESTbot
server address (`localhost` on port 9080) and that is followed by a forward
slash, the word `establish_session` (the command name), and the password
(which is `pass` by default). If this is not formatted correctly, an argument
error usually is returned.

`establish_session` takes a bit of time to process, as it makes sure the bot
has been able to log in before it returns a session identification key. When
it does, store this key somewhere. You use this session id as an argument for
any bot-specific methods (defined in plugins.)

Exit runs the same, but takes `session` as an argument with the session_id
as the value of the argument. This will return a success/failure response.

Explore the source code of the framework and the example plugins that come
with it to play around with commands... or check out the wiki for more
information (see [LINKS](LINKS.md)).

_Original documentation written by an anonymous collaborator at Pleiades; several changes were been made by Gwyneth Llewelyn, but trying to keep the original, light-hearted style of explaining things (gwyneth 20211121)._