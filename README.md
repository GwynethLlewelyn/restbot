![RESTbot logo](assets/images/RESTbot-logo.png)

# README File - RESTBot

## Revisions
- Written August 9, 2007 -  Pleiades Consulting, Inc
- Revised June 5, 2010 - Gwyneth Llewelyn (notes on compilation)
- Revised November 21, 2021 - Gwyneth Llewelyn (updates for a lot of things)
- Dramatic changes made on November 23, 2021 - Gwyneth Llewelyn

# Overview

RESTBot is a C# webserver that uses [RESTful transactions](https://en.wikipedia.org/wiki/Representational_state_transfer) to interact with a _bot_ in [Second Life](https://secondlife.com) or [OpenSimulator](http://opensimulator.org). This bot (or a collection of bots) is started through a REST command and can be stopped the same way. The software is extremely modular, and is designed to easily accept plugins that developers can write (in C#) to add more complex interaction between a web application and a Second Life® bot (a non-human-controlled avatar). More information about Plugins is on the [wiki](https://github.com/GwynethLlewelyn/restbot/wiki).

_**Note:** The wiki is still under reformulation and may not correctly represent the state-of-the-art of RESTbot (gwyneth 20211123)_

## Preparing the compilation environment

### Under Windows (not tested)

Probably you only need to get the Community version of [Visual Studio](https://visualstudio.microsoft.com/), which should install everything you need.

Make sure you select a version that supports **.NET** 6.0.

### Under macOS:

There are basically three possible ways to get RESTbot to compile and run. Let's start with the more straightforward one, which only relies on the command line.

#### Get Mono

Install Mono. To save time (in fixing dependencies...) just use [Homebrew](https://brew.sh):

```bash
brew install mono
export MONO_GAC_PREFIX="/usr/local"
```

You may wish to add the latter to your `~/.bashrc` or equivalent.

_**Note:** There is a chance that a working Mono environment is not even needed any more! (gwyneth 20211129)_

If you wish to avoid later errors regarding the lack of `libgdiplus`, you ought to install it as well:

```bash
brew install mono-libgdiplus
```

#### Add the .NET environment

```bash
brew install dotnet-sdk-preview
export DOTNET_ROOT=/usr/local/share/dotnet/
```

At the time of writing, you need **.NET** 6.0 (the latest and greatest!). This requires installing the *preview* version from Homebrew; the default long-term service (LTS) version is still 5.X, although Microsoft admits that it will switch to 6.0 as soon as it's feasible. This also means that, once Homebrew catches up with Microsoft, you might need to do a `brew remove dotnet-sdk-preview` and do a `brew install dotnet-sdk` instead.

#### Compile it

```bash
cd /path/where/you/have/installed/restbot
dotnet msbuild
```

`dotnet` is Microsoft's megatool for managing **.NET** packages and (new) applications; the above command invokes `msbuild`, Microsoft's all-in-one building tool, which, in turn, will handle code dependencies, figure out what to compile, invoke [Microsoft's Roslyn compiler](https://github.com/dotnet/roslyn), and generate an executable that runs *directly* under macOS.

Optionally, you may wish to run `dotnet msbuild -m` to get the Microsoft compiler to use several cores in parallel (by default, everything is compiled sequentially).

There will be quite a lot of warnings, mostly due to the lack of XML comments on the (many) functions. This is a work-in-progress — these days, it seems that it's almost 'mandatory' to have all functions properly documented, so that an automatic documentation file (`RESTbot.xml`) is generated. This is a Good Thing, but, remember, this code was written mostly a decade ago, when good practices such as documenting code were not _enforced_, so it means going back through all those old functions and figuring out what each of them does... thus the warnings.

There are also a few warnings not related to documentation, basically some stricter checking on initialisation values. These will also be addressed over time; the main goal, for now, is to get it compiled and running.

Once compiled, the new executable should be generated under `testbot-bin/net6.0`, `RESTbot`. This is a standalone executable — actually, the runtime which will load `RESTbot.dll` (also compiled on the same directory). The amazing thing is that, these days, Microsoft doesn't even require people to _use_ Mono at all — they get their Roslyn compiler to generate native code wrapped around the DLL. What could be more amazing? (Note to self: grumble to the OpenSimulator guys to get them to do the same.) We truly live in interesting times.

Also note that if you managed to install the pre-requisites using Homebrew, you _should_ also be able to compile things for your brand new Apple Silicon CPU. Aye, Microsoft is _that_ nice with their latest-generation Roslyn compiler.

Before you launch RESTbot, you'll need to create a `configuration.xml` file with the parameters for your grid, and where the 'bot should start. Go ahead and jump to [the configuration](#configuration) section.

**Note:** To clean up everything and start a compilation from scratch, you can use the following target:

```bash
dotnet msbuild /t:CleanOutDir
```

This should be cross-platform-safe (as far as I can test it, of course!).

#### ... or you can just use Visual Studio for Mac!

You wouldn't believe the tools that Microsoft has come up with to persuade non-Windows users to surrender to their integrated environment. These days, you can get [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/) — *not* the free and open-source Visual Studio *Code*, which is a humble code editor, albeit one with a megaton of features; but rather Microsoft's own full-blown IDE with all the bells and whistles, and which looks like the Windows equivalent, but with a more Apple-ish look.

In theory, all you need is to install it — please make sure you get a version which works with **.NET** _6.0_; currently, this is **Visual Studio 2022 for Mac Preview** but expect Microsoft to move on to a non-preview (i.e. *stable*) version soon. Earlier versions will *not* work.

If you want to do actual development, especially starting the project from scratch (to adapt it to different environments, for example, such as virtual machines, Docker containers, Azure or even AWS cloud services, whatever...) then I really recommend using Visual Studio for Mac. In fact, I wasn't able to generate all the required files manually, without recourse to Visual Studio; VS basically 'prepares' everything properly, starting from the included `.csproj` (which gets changed as new references get included and others are removed), to get you a 'working' project. Once that project _is_ generated properly, _then_ you can use the command-line tools.

#### The third way...

So you're not really fond of IDEs, much less Microsoft's? You're not alone. If you don't trust Homebrew, either — or are used to a different package management system (such as [MacPorts](https://www.macports.org/)) and don't want Homebrew to interfere with it — then Microsoft gives you a further choice: just [install `dotnet` directly from Microsoft](https://dotnet.microsoft.com/download). This can be done just for the current user (in which case everything will be installed under `~/.dotnet/`) or globally for all users.

Note that, as said before, _I_ could not figure out how to pre-generate all project files, with all correctly included and imported things, just via the command-line tools. But _once_ these are generated and in place (as they are when you download/clone this project), _then_ any set of command-line tools that support NET 6.0 ought to do a successful compilation.

### Under Linux:

It's basically the same as using the command-line tools under macOS; you can follow those instructions if you wish. These days, Homebrew also works under Linux, so the instructions would be the same; but possibly you will prefer to run your 'native' package manager, be it `apt` (Debian/Linux) or `yum` (Fedora, CentOS, RedHat) or whatever is fashionable these days. You'll have to check what versions of Mono are available; remember that you'll need a 'developer' edition, and don't forget to double-check that `msbuild` and `csc` (the Microsoft Roslyn C# compiler) comes as part of the package as well.

Sadly, at the time of writing, there is no Visual Studio desktop IDE for Linux. You can, however, use many of Microsoft's tools being called from the command line and integrate those in Visual Studio *Code*, which *is* available for Linux as well.

Again, like under macOS, Microsoft's Roslyn compiler will generate a Linux-native executable which will be able to launch `RESTbot.dll`, without the need of using Mono (and that executable ought to be distro-agnostic and work under _all_ of them, naturally including Microsoft's own Linux distro). I've tested it within a x86 environment, but allegedly Microsoft also supports ARM. I _do_ have a Linux ARM box to do some testing with it as well, but I haven't gotten the time to do so.

# Configuration

The default configuration file is called `configuration.xml`and should be located in the same directory as the `RESTbot` executable; under `assets/configuration`, I've provided a `configuration.xml.sample` — make sure you copy this file to `configuration.xml` and change it to reflect your (real) data.

The configuration file is very flexible... if a configuration element is not defined in the file, it will revert to its default instead of stopping... so a custom configuration file can either define every single variable, or only contain one variable definition. If you want to go ahead and play with a RESTbot server now, you can go ahead and skip ahead to [Running](#running-windows) (by default, the server listens to `localhost` connections on port 9080).

You can play around with the settings in `configuration.xml` at any time, you just have to restart the server in order for any changes to take effect. The options are fairly self explanatory, but if you need help you can always refer to the [wiki](https://github.com/GwynethLlewelyn/restbot/wiki) (see [LINKS](LINKS.md)).

# Basic Commands

Once RESTBot is running, you can connect to it via HTTP REST commands. For now, I will use `curl` on Linux to demonstrate basic commands you need to know to get running with RESTbot.

First, I am running RESTbot on a Ubuntu Linux machine. It uses the default network settings (`localhost` on port 9080).

A note on how commands are passed to RESTbot for processing: server commands require a password, which is set in the security block of the configuration file. The default password is (yes, I know) `pass`.

The following is a list of working server commands:

* `establish_session`: this starts up a RESTbot
* `exit`: this stops a RESTbot

_Note: There are plenty more commands these days that are also fully functional (gwyneth 20211121); these are provided by so-called 'RESTbot plugins' and the Wiki holds some documentation on them,_

Server commands are defined in the URL and arguments are passed through POST.

The most important command you will use is `establish_session` so I will first introduce you to its parameters.

Let's say we have a hypothetical bot account whose name is `Restbot Resident` and uses the password `omgrestbot` to login. To log into this bot, we would issue this command in curl:

```bash
curl http://localhost:9080/establish_session/pass -d first=Restbot -d last=Resident -d pass=77e854984fd6a73ece3aedab7ee9e21c
```

Some things to note in this command... everything after `-d` is a **post field**. In reality, _all_ of the post fields are spliced together as a single string separated by &'s. Also, the password **must** be `md5`'d. Lastly, notice how the `establish_session` URL is formatted. http://localhost:9080 is the RESTbot server address (`localhost` on port 9080) and that is followed by a forward slash, the word `establish_session` (the command name), and the password (which is `pass` by default). If this is not formatted correctly, an argument
error usually is returned.

`establish_session` takes a bit of time to process, as it makes sure the bot has been able to log in before it returns a session identification key. When it does, store this key somewhere. You use this session id as an argument for any bot-specific methods (defined by RESTbot plugins).

`exit` runs the same, but takes `session` as an argument with the session_id as the value of the argument. This will return a success/failure response.

Explore the source code of the framework and the example plugins that come with it to play around with commands... or check out the wiki for more information (see [LINKS](LINKS.md)).

# The Magic Behind Everything

At the root of RESTbot lies a community-managed project, originally known as *libSL*, later renamed to *libopenmetaverse* (because of trademark issues), and currently published via Microsoft's NuGet package repository under the name *LibreMetaverse*. This is an open-source (originally reverse-engineered) library of Linden Lab's communication protocol between a SL viewer and their grid, written in C#.

You can read more about the history of *LibreMetaverse* on this project's [wiki](https://github.com/GwynethLlewelyn/restbot/wiki/History) (a work in progress...)

RESTbot is therefore just 'wrapper' code that launches a mini-webserver and exposes a RESTful API to selected LibreMetaverse commands.

_Original documentation written by an anonymous collaborator at Pleiades; several changes were been made by Gwyneth Llewelyn, but trying to keep the original, light-hearted style of explaining things (gwyneth 20211121)._