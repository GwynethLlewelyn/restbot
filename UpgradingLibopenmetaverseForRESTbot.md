# The problem #

RESTbot was designed to allow "easy" upgrade of libopenmetaverse by having it on a separate directory. The problem is that libopenmetaverse goes through several evolutions and it soon its API becomes incompatible with software developed for past versions

The instructions below assume Mac OS X or Linux. You're on your own if you're using Windows (sorry!).

# Steps for the upgrade #

  * If you're using SVN: delete the _libopenmetaverse_ folder under RESTbot and force a commit.
  * Go to a _different_ directory and get the latest code from libopenmetaverse using: **svn co https://svn.github.com/openmetaversefoundation/libopenmetaverse libopenmetaverse** (see http://lib.openmetaverse.org/wiki/Download)
  * Now delete the SVN tags from libopenmetaverse (they will conflict with _your_ build) by going to the freshly created _libopenmetaverse_ and running **find . -name '\.svn' -print -exec rm -rf .svn {} \;**
  * Move this _libopenmetaverse_ folder properly under RESTbot's own folder
  * Go to _libopenmetaverse_ and do **./runprebuild.sh nant**
  * Now commit the full changes on _your_ SVN