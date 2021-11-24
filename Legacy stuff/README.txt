README File - RESTBot
Written August 9, 2007 -  Pleiades Consulting, Inc
Revised June 5, 2010 - Gwyneth Llewelyn (notes on compilation)

+-------------------------+
|        OVERVIEW         |
+-------------------------+

RESTBot is a C# webserver that uses RESTful transactions to interact with a 
"bot" in Second Life. This bot (or a collection of bots) is started through
a REST command and can be stopped the same way. The software is extremely 
modular, and is designed to easily accept plugins that developers can write
(in C#) to add more complex interaction between a web application and a 
Second Life Bot. More information about Plugins is on the wiki. (See LINKS)

+-------------------------+
|   COMPILING - WINDOWS   |
+-------------------------+

Compiling RESTBot is very easy on a Windows (XP Preferred) machine. First, you
need Visual Studio 2007 (Express Edition is Perfect.) The great thing about
this IDE, is that you can download the Express edition for FREE! (See LINKS)
We used Visual Studio C# Express, and we will use this IDE for any Windows-
based instructions. 

To compile under windows, open up VS and open the RESTBOT-VS2005-sln.sln file 
located in the project's root directory. This will open up restbot's solution 
in the IDE. 

In the Solution Explorer, make sure that the "RESTBOT-VS2005-csproj" project
is bold (the startup project) - if it is not, right click the project and 
select "Set as Startup Project"

To build, all you have to do is go to Build -> Build Solution (or you can 
press F6)

On the status bar (the text at the bottom of the window of VS) - you should see
"Build succeeded", which means that the compilation was complete. You can now
skip to the CONFIGURATION section of this readme file.

Note: this version was NOT tested under Windows. Caveat utilitor.

+-------------------------+
|    COMPILING - LINUX    |
+-------------------------+

In order to compile on a linux machine, you need at least the following 
applications installed on your system:

* nANT
* Mono 1.2.4 or better (with .NET 2.0 package)
* gmcs (known as the mono-gmcs package on Debian)

Once all of that is installed, cd to the main directory of your copy of restbot
and run "nant" in the command line. A lot of gibberish will run across the 
screen. This usually takes about 20 seconds or so.. and hopefully you will see 
"BUILD SUCCEEDED" when it is done. Yay! Go on to the CONFIGURATION section.

Note: if the compilation fails, try the following. First go to the ./libopenmetaverse folder. Run ./runprebuild.sh and do a nant clean followed by nant. This should compile libopenmetaverse cleanly. Go back to the root directory and run nant again. This time it should work properly.

+-------------------------+
|      CONFIGURATION      |
+-------------------------+

Configuration is simple with our XML compatibility in RESTBot. Configuration
is read at startup of the restbot server, so you have to have the configuration
file along with the restbot.exe binary. The default configuration file is
configuration.xml located in ./restbot-bin. The configuration file is very 
flexible.. if a configuration element is not defined in the file, it will revert
to its default instead of stopping.. so a custom configuration file can either 
define every single variable, or only contain one variable definition. If you
want to go ahead and play with a restbot server now, you can go ahead and skip
ahead to RUNNING (By default, the server listens to localhost connections on
port 9080)

You can play around with the settings in configuration.xml at any time, you just
have to restart the server in order for any changes to take effect. The options
are fairly self explanatory, but if you need help you can always refer to the 
wiki (see LINKS)

+-------------------------+
|    RUNNING - WINDOWS    |
+-------------------------+

Running on windows is very easy. Just navigate to the restbot-bin folder and
then execute the restbot.exe file. This runs in the command line, so you will
be presented with a black console with information / debug text. Errors will
appear in yellow, warnings will appear in read, and informational statements
will appear in white. If you see blue text, that is OK too - it just means one
of our developers was lazy and didn't remove his debug from his code. :)
To stop, you can just exit out of the console and the server will shutdown.

+-------------------------+
|    RUNNING - LINUX      |
+-------------------------+

Running on linux is easy, once you get it working the first time. 'cd' to the 
restbot-bin directory and run 'mono restbot.exe' - if you get any problems,
try running 'sudo mono restbot.exe' or run the above command in superuser mode.

If you have any more questions, visit the wiki (see LINKS) where you can search
for a solution or find contact information for professional help.

+-------------------------+
|     BASIC COMMANDS      |
+-------------------------+

Once RESTBot is running, you can connect to it via HTTP REST commands. For now,
I will use curl on linux to demonstrate basic commands you need to know to get
running with restbot.

First, I am running restbot on Debian linux machine. It is using the default
network settings (localhost on port 9080)

A note on how commands are passed to restbot for processing. Server commands 
require a password, which is set in the security block of the configuration
file. The default password is (yes,I know) "pass" The following is a list
of working server commands:

* establish_session: this starts up a restbot
* exit: this stops a restbot

Server commands are defined in the URL and arguments are passed through POST.
The most important command you will use is "establish_session" so I will first
introduce you to those parameters.

Lets say we have a hypothetical bot account whos name is "Restbot Zaius" and 
uses the password "omgrestbot" to login. To log into this bot, we would issue
this command in curl:
curl http://localhost:9080/establish_session/pass -d first=Restbot -d last=Zaius -d pass=77e854984fd6a73ece3aedab7ee9e21c

Some things to note in this command.. everything after -d is a post field. In
reality, all of the post fields are spliced together as a single string 
seperated by &'s. Also, the password *must* be md5'd. Lastly, notice how the
establish_session URL is formatted. http://localhost:9080 is the restbot
server address (localhost on port 9080) and that is followed by a forward
slash, the word "establish_session" (the command name), and the password 
(which is pass by default.) If this is not formatted correctly, an argument
error usually is returned. 

Establish_session takes a bit of time to process, as it makes sure the bot 
has been able to log in before it returns a session identification key. When
it does, store this key somewhere. You use this session id as an argument for 
any bot specific methods (defined in plugins.)

Exit runs the same, but takes "session" as an argument with the session_id
as the value of the argument. This will return a success / failure response.

Explore the source code of the framework and the example plugins that come
with it to play around with commands.. or check out the wiki for more 
information (see LINKS)




