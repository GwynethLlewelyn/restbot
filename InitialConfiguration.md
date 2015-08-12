# Compiling #

If you're upgrading libopenmetaverse, make sure you read [UpgradingLibopenmetaverseForRESTbot](UpgradingLibopenmetaverseForRESTbot.md) first.

Under Linux/Mac OS X:

If not, you might need to prebuild libopenmetaverse the first time you run it. Go to the libopenmetaverse directory first and do **./runprebuild.sh nant** This should pre-generate the necessary build files.

Then you can go to the root of your installation and run **nant**, which should compile everything.

# Logging in to alternate grids #

The actual grid configuration parameters are on **./restbot-bin/configuration.xml**. Just change what is between

```
<loginuri>https://login.agni.lindenlab.com/cgi-bin/login.cgi</loginuri>
```

to the URL login setting for your own grid.

Currently, one RESTbot can only connect to a single grid. But you can launch several different RESTbots on the same server, at different ports, one per different grid, if you wish.