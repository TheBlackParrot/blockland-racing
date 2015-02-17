// using methods from the default SpeedKarts gamemode.

if($pref::Server::SpeedKart::RoundLimit $= "")
{
   $pref::Server::SpeedKart::RoundLimit = 8;
}

$SK::Initialized = false;

function SK_BuildTrackList()
{
   //
   %pattern = "Add-Ons/Racing_*/save.bls";
   
   $SK::numTracks = 0;
   
   %file = findFirstFile(%pattern);
   while(%file !$= "")
   {
      $SK::Track[$SK::numTracks] = %file;
      $SK::numTracks++;

      %file = findNextFile(%pattern);
   }
}

function SK_DumpTrackList()
{
   echo("");
   if($SK::numTracks == 1)
      echo("1 track");
   else
      echo($SK::numTracks @ " tracks");
   for(%i = 0; %i < $SK::numTracks; %i++)
   {
      %displayName = $SK::Track[%i];
      %displayName = strReplace(%displayName, "Add-Ons/Racing_", "");
      %displayName = strReplace(%displayName, "/save.bls", "");
      %displayName = strReplace(%displayName, "_", " ");

      if(%i == $SK::CurrentTrack)
         echo(" >" @ %displayName);
      else
         echo("  " @ %displayName);
   }
   echo("");
}

function SK_NextTrack()
{
   $SK::CurrentTrack = mFloor($SK::CurrentTrack);
   $SK::CurrentTrack++;
   $SK::CurrentTrack = $SK::CurrentTrack % $SK::numTracks;

   $SK::ResetCount = 0;

   SK_LoadTrack_Phase1($SK::Track[$SK::CurrentTrack]);
}

function SK_LoadTrack_Phase1(%filename)
{
   //suspend minigame resets
   $SK::MapChange = 1;

   //put everyone in observer mode
   %mg = $DefaultMiniGame;
   if(!isObject(%mg))
   {
      error("ERROR: SK_LoadTrack( " @ %filename  @ " ) - default minigame does not exist");
      return;
   }
   for(%i = 0; %i < %mg.numMembers; %i++)
   {
      %client = %mg.member[%i];
      %player = %client.player;
      if(isObject(%player))
         %player.delete();

      %camera = %client.camera;
      %camera.setFlyMode();
      %camera.mode = "Observer";
      %client.setControlObject(%camera);
   }
   
   //clear all bricks 
   // note: this function is deferred, so we'll have to set a callback to be triggered when it's done
   BrickGroup_888888.chaindeletecallback = "SK_LoadTrack_Phase2(\"" @ %filename @ "\");";
   BrickGroup_888888.chaindeleteall();
}

function SK_LoadTrack_Phase2(%filename)
{
   $Racing::Laps = 0;
   $Racing::LapTolerance = 0;

   echo("Loading speedkart track " @ %filename);

   %displayName = %filename;
   %displayName = strReplace(%displayName, "Add-Ons/Racing_", "");
   %displayName = strReplace(%displayName, "/save.bls", "");
   %displayName = strReplace(%displayName, "_", " ");
   
   %loadMsg = "\c5Now loading \c6" @ %displayName;

   //read and display credits file, if it exists
   // limited to one line
   %creditsFilename = filePath(%fileName) @ "/credits.txt";
   if(isFile(%creditsFilename))
   {
      %file = new FileObject();
      %file.openforRead(%creditsFilename);

      %line = %file.readLine();
      %line = stripMLControlChars(%line);
      %loadMsg = %loadMsg @ "\c5, created by \c3" @ %line;

      %file.close();
      %file.delete();
   }

   messageAll('', %loadMsg);

   //load environment if it exists
   %envFile = filePath(%fileName) @ "/environment.txt"; 
   if(isFile(%envFile))
   {  
      //echo("parsing env file " @ %envFile);
      //usage: GameModeGuiServer::ParseGameModeFile(%filename, %append);
      //if %append == 0, all minigame variables will be cleared 
      %res = GameModeGuiServer::ParseGameModeFile(%envFile, 1);

      EnvGuiServer::getIdxFromFilenames();
      EnvGuiServer::SetSimpleMode();

      if(!$EnvGuiServer::SimpleMode)     
      {
         EnvGuiServer::fillAdvancedVarsFromSimple();
         EnvGuiServer::SetAdvancedMode();
      }
   }

   %settingsFile = filePath(%filename) @ "/settings.txt";
   if(isFile(%settingsFile)) {
      %file = new FileObject();
      %file.openForRead(%settingsFile);

      $Racing::Laps = getField(%file.readLine(),1);
      $Racing::LapTolerance = getField(%file.readLine(),1);
   } else {
      messageAll('',"You're missing a settings file!" SPC %settingsFile);
      SK_NextTrack();
      return;
   }

   if(!$Racing::Laps || !$Racing::LapTolerance) {
      messageAll('',"Something isn't defined in the settings file (" @ %settingsFile @ "), please make sure you're using tabs and that you have Laps and LapTolerance defined.");
   }
   
   //load save file
   schedule(10, 0, serverDirectSaveFileLoad, %fileName, 3, "", 2, 1);
}

function serverCmdTrackList(%client)
{
   for(%i = 0; %i < $SK::numTracks; %i++)
   {
      %displayName = $SK::Track[%i];
      %displayName = strReplace(%displayName, "Add-Ons/Racing_", "");
      %displayName = strReplace(%displayName, "/save.bls", "");
      %displayName = strReplace(%displayName, "_", " ");

      if(%i == $SK::CurrentTrack)
         messageClient(%client, '', " >" @ %i @ ". \c6" @ %displayName);
      else
         messageClient(%client, '', "  " @ %i @ ". \c6" @ %displayName);
   }
}

package GameModeSpeedKartPackage
{
   //this is called when save loading finishes 
   function GameModeInitialResetCheck()
   {
      Parent::GameModeInitialResetCheck();

      //if there is no track list, attempt to create it
      if($SK::numTracks == 0)
         SK_BuildTrackList();
      
      //if tracklist is still empty, there are no tracks
      if($SK::numTracks == 0)
      {
         messageAll('', "\c5No SpeedKart tracks available!");
         return;
      }

      if($SK::Initialized)
         return;

      $SK::Initialized = true;
      $SK::CurrentTrack = -1;
            
      SK_NextTrack();
   }

   //when we're done loading a new track, reset the minigame
   function ServerLoadSaveFile_End()
   {
      Parent::ServerLoadSaveFile_End();

      //new track has loaded, reset minigame
      if($DefaultMiniGame.numMembers > 0) //don't bother if no one is here (this also prevents starting at round 2 on server creation)
         $DefaultMiniGame.scheduleReset(); //don't do it instantly, to give people a little bit of time to ghost

      setRequiredTrackBricks();
   }

   //when vehicle spawns, it cannot move (event must enable it)
   //this solves the driving through the garage problem
   function WheeledVehicleData::onAdd(%data,%obj)
   {
      Parent::onAdd(%data, %obj);

      for(%i = 0; %i < %data.numWheels; %i++)
         %obj.setWheelPowered(%i,0);
   }

   function MiniGameSO::Reset(%obj, %client)
   {
      //make sure this value is an number
      $pref::Server::SpeedKart::RoundLimit = mFloor($pref::Server::SpeedKart::RoundLimit);

      //handle our race time hack
      %obj.raceStartTime = 0;

      //count number of minigame resets, when we reach the limit, go to next track
      if(%obj.numMembers >= 0)
      {
         $SK::ResetCount++;
      }

      if($SK::ResetCount > $pref::Server::SpeedKart::RoundLimit)
      {
         $SK::ResetCount = 0;
         SK_NextTrack();
      }
      else
      {
         messageAll('', "\c5Beginning round " @ $SK::ResetCount @ " of " @ $pref::Server::SpeedKart::RoundLimit);
         Parent::Reset(%obj, %client);
      }
   }  
};
activatePackage(GameModeSpeedKartPackage);


//load the actual karts
// these are a feature locked version of the karts from a while ago
exec("Add-Ons/GameMode_SpeedKart/karts/speedKart.cs");


