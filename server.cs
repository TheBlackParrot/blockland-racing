exec("./load_functions.cs");

function fxDTSBrick::getTopBrick(%this) {
	%upbrick = %this;
 
	while(isObject(%upbrick.getUpBrick(0))) {
		%upbrick = %upbrick.getUpBrick(0);
	}
 
	return %upbrick;
}

function setRequiredTrackBricks() {
	// eventually get around to setting a specific color for track bricks via track info
	for(%i=0;%i<BrickGroup_888888.getCount();%i++) {
		%brick = BrickGroup_888888.getObject(%i);
		if(%brick.colorID != 7) {
			continue;
		}
		if(%brick.colorID == 7) {
			if(!%brick.getNumUpBricks()) {
				%brick.isTrackBrick = 1;
				%count++;
				continue;
			} else {
				%selection = %brick.getTopBrick();
				if(%selection.isTrackBrick || %selection.colorID != 7) {
					%brick.isTrackBrick = 1;
					%count++;
				} else {
					%selection.isTrackBrick = 1;
					%count++;
				}
			}
		}
	}
	messageAll('',"Set" SPC getTrackBrickCount() SPC "track bricks.");
}

function servercmdPreviewTrackBricks(%this) {
	if(%this.bl_id != getNumKeyID()) {
		return;
	}
	for(%i=0;%i<BrickGroup_888888.getCount();%i++) {
		%brick = BrickGroup_888888.getObject(%i);
		if(%brick.isTrackBrick) {
			%brick.setColor(1);
			%brick.schedule(3000,setColor,7);
		}
	}
}

function getTrackBrickCount() {
	for(%i=0;%i<BrickGroup_888888.getCount();%i++) {
		%brick = BrickGroup_888888.getObject(%i);
		if(%brick.isTrackBrick) {
			%count++;
		}
	}
	return %count || 0;
}

function serverCmdUnsetTrackBricks(%this) {
	if(%this.bl_id != getNumKeyID()) {
		return;
	}
	for(%i=0;%i<BrickGroup_888888.getCount();%i++) {
		%brick = BrickGroup_888888.getObject(%i);
		if(%brick.isTrackBrick) {
			%brick.setColor(7);
			%brick.setColorFX(3);
			%brick.schedule(300,setColorFX,0);
			%brick.isTrackBrick = 0;
			%count++;
		}
	}
	messageClient(%this,'',"Unset" SPC %count SPC "track bricks.");
}

function serverCmdFlipOver(%this) {
	if(getSimTime() - %this.lastFlip < 10000) {
		messageClient(%client,'',"You must wait another" SPC mFloor(getSimTime() - %this.lastFlip/100)/10 SPC "seconds to flip back over.");
		return;
	}

	%obj = %this.getControlObject();
	if(%obj.lastTouched) {
		%selection = %obj.lastTouched;
		%x = getWord(%selection.getPosition(),0);
		%y = getWord(%selection.getPosition(),1);
		%z = getWord(%selection.getPosition(),2);
		%z_add = %z + %selection.getDatablock().brickSizeZ/6;
		%rot = getWord(%obj.getTransform(),6);

		%obj.setVelocity("0 0 0");
		%obj.setTransform(getWords(%obj.getPosition(),0,1) SPC getWord(%obj.getPosition(),2) + 3 SPC "0 0 1" SPC %rot);
		%obj.setVelocity("0 0 0");
		//%this.lastFlip = getSimTime();
	} else {
		messageClient(%client,'',"You do not appear to be controlling a vehicle directly.");
	}
}

function WheeledVehicle::isGrounded(%this) {
	initContainerBoxSearch(%this.getPosition(), "0.5 0.5 -1", $TypeMasks::FXBrickObjectType);
	while((%targetObject = containerSearchNext()) != 0 && isObject(%targetObject)) {
		return 1;
	}
	return 0;
}

function WheeledVehicle::doBrickCheckLoop(%this) {
	cancel(%this.checkLoop);
	%this.checkLoop = %this.schedule(200,doBrickCheckLoop);
	if(!%this.laps) {
		%this.laps = 1;
		%this.startedLap = getSimTime();
	}

	initContainerBoxSearch(%this.getPosition(), "40 40 40", $TypeMasks::FXBrickObjectType);
	while((%targetObject = containerSearchNext()) != 0 && isObject(%targetObject)) {
		if(%targetObject.isTrackBrick) {
			if(stripos(%this.hasTouched,%targetObject) == -1) {
				%this.hasTouched = %this.hasTouched @ %targetObject;
				%this.touchedCount++;
				%this.totalTouchedCount++;
				if(%this.isGrounded()) {
					%this.lastTouched = %targetObject;
					%this.lastTransform = %this.getTransform();
				}
				//talk("TOUCHED" SPC %targetObject SPC "[" @ %this.touchedCount @ "]");
				%targetObject.setColor(0);
				%targetObject.schedule(400,setColor,7);
			}
		}
	}

	initContainerBoxSearch(%this.getPosition(), "1 1 1", $TypeMasks::FXBrickObjectType);
	while((%targetObject = containerSearchNext()) != 0 && isObject(%targetObject)) {
		if(%targetObject.getName() $= "_race_lapbrick") {
			// fuzzy collision, oh boy
			// range though should be high enough in the first box search
			%required_amount = mCeil(getTrackBrickCount()/$Racing::LapTolerance);
			if(%this.touchedCount >= %required_amount) {
				%this.hasTouched = "";
				%this.touchedCount = 0;
				%this.lapTime[%this.laps] = getSimTime() - %this.startedLap;
				if(%this.laps >= $Racing::Laps) {
					%client = %this.getControllingClient();
					messageAll('',"\c3" @ %client.name SPC "\c6has finished in\c3" SPC getPositionString(%this.getApproximatePosition()) SPC "\c6with a time of\c3" SPC getTimeString((getSimTime() - $Racing::StartedAt)/1000) @ "\c6!");
					%this.inRace = 0;
					$Racing::Finished++;
					for(%i=0;%i<$DefaultMinigame.numMembers;%i++) {
						%mem = $DefaultMinigame.member[%i];
						if(isObject(%mem.player)) {
							if(%mem.player.inRace) {
								%not_finished++;
							}
						}
					}
					if(!%not_finished) {
						$DefaultMinigame.messageAll('',"End of round" SPC $SK::ResetCount @ ". A new round will begin in 8 seconds.");
						$DefaultMinigame.schedule(8000,reset);
					}
					// YEAH EXPLOSIONS
					%this.schedule(100,finalExplosion);
				}
				if(isObject(%this)) {
					%this.laps++;
					%this.startedLap = getSimTime();
					talk("LAP" SPC %this.laps SPC %this.getControllingClient().name);
				}
			}
		}
	}

	// really the only reason why this is here is to "predict" race positions and prevent you lame cheaters from cheating
}

function Vehicle::getApproximatePosition(%this) {
	for(%i=0;%i<$DefaultMinigame.numMembers;%i++) {
		%client = $DefaultMinigame.member[%i];
		if(isObject(%client.player)) {
			if(%client.player.totalTouchedCount >= %this.touchedCount && %client != %this.getControllingClient()) {
				%pos++;
			}
		}
	}
	return %pos + 1 + $Racing::Finished;
}

function getPositionString(%num) {
	if(strLen(%num)-2 >= 0) {
		%ident = getSubStr(%num,strLen(%num)-2,2);
	} else {
		%ident = %num;
	}
	if(%ident >= 10 && %ident < 20) {
		return %num @ "th";
	}

	%ident = getSubStr(%num,strLen(%num)-1,1);
	switch(%ident) {
		case 1:
			return %num @ "st";
		case 2:
			return %num @ "nd";
		case 3:
			return %num @ "rd";
		default:
			return %num @ "th";	
	}
}

function GameConnection::doStatLoop(%this) {
	cancel(%this.statLoop);
	%this.statLoop = %this.schedule(100,doStatLoop);

	%player = %this.player;
	%time[1] = getSimTime() - %player.startedLap;
	%time[2] = getSimTime() - $Racing::StartedAt;

	for(%i=1;%i<=2;%i++) {
		if(strLen(%time[%i])-3 >= 0) {
			%time_ms[%i] = getSubStr(%time[%i],strLen(%time[%i])-3,1);
		}
		if(%time_ms[%i] $= "") {
			%time_ms[%i] = 0;
		}
	}

	%str[1] = "<font:Arial Bold:48><color:ffee00>" @ getPositionString(%this.player.getApproximatePosition());
	%str[2] = "<font:Arial Bold:24><just:center><color:00ff00>Lap" SPC %this.player.laps SPC "/" SPC $Racing::Laps;
	%str[3] = "<font:Arial Bold:24><just:right><color:ffffff>" @ getTimeString(mFloor(%time[2]/1000)) @ "." @ %time_ms[2] @ " <font:Arial Bold:16><color:aaaaaa>[" @ getTimeString(mFloor(%time[1]/1000)) @ "." @ %time_ms[1] @ "]";

	%this.bottomPrint(%str[1] @ %str[2] @ %str[3]);
	//%this.bottomPrint(%time);
}

function MinigameSO::startRace(%this) {
	%this.centerPrintAll("\c2<font:Arial Bold:48>GO!",3);
	$Racing::HasStarted = 1;
	$Racing::Finished = 0;
	$Racing::StartedAt = getSimTime();

	for(%i=0;%i<%this.numMembers;%i++) {
		%client = %this.member[%i];

		if(isObject(%client.player)) {
			%player = %client.player;

			for(%i=0;%i<4;%i++) {
				%player.setWheelPowered(%i,1);
				%player.doBrickCheckLoop();
				%client.doStatLoop();
			}
		}
	}
}

package GoKartPackage {
	function WheeledVehicleData::onAdd(%data,%obj) {
		Parent::onAdd(%data, %obj);

		//making sure of things
		%obj.lastTouched = "";
		%obj.lastTransform = "";
		%obj.laps = 1;
		%obj.touchedCount = 0;
		%obj.totalTouchedCount = 0;
		%obj.hasTouched = "";
	}
	function VehicleData::onEnterLiquid(%data, %obj, %coverage, %type) {
		Parent::onEnterLiquid(%data, %obj, %coverage, %type);

		if(%obj.lastTransform !$= "") {
			echo("LAST TRANSFORM EXISTS");
			%rot = getWord(%obj.lastTransform,6);
			
			%obj.setVelocity("0 0 0");
			%obj.setTransform(getWords(%obj.lastTransform,0,2) SPC "0 0 1" SPC %rot);
			%obj.setVelocity("0 0 0");
		} else {
			// something has gone wrong, just explode them.
			echo("LAST TRANSFORM DOES NOT EXIST");
			%obj.finalExplosion();
		}
	}

	function GameConnection::spawnPlayer(%this) {
		parent::spawnPlayer(%this);
		if(isObject(%this.minigame)) {
			if(!$Racing::HasStarted) {
				for(%i=1;%i<=42;%i++) {
					%brick = "_vehicle_spawn" @ %i;
					if(!%brick.isUsed) {
						%this.spawnBrick = %brick;
						%brick.isUsed = 1;
						if(!%brick.vehicle) {
							%brick.setVehicle(SpeedKartclassicgtVehicle.getID());
							if(!isObject(%brick.vehicle)) {
								%brick.respawnVehicle();
							}
						} else {
							%brick.respawnVehicle();
						}
						%this.player.delete();
						%this.player = %brick.vehicle;
						%brick.vehicle.inRace = 1;
						$Racing::PlayerAmount++;
						%this.setControlObject(%brick.vehicle);
						return;
					} else {
						if(isObject(%brick)) {
							if(%this.spawnBrick.getID() == %brick.getID()) {
								if(!%brick.vehicle) {
									%brick.setVehicle(SpeedKartclassicgtVehicle.getID());
									if(!isObject(%brick.vehicle)) {
										%brick.respawnVehicle();
									}
								} else {
									%brick.respawnVehicle();
								}
								%this.player.delete();
								%this.player = %brick.vehicle;
								%brick.vehicle.inRace = 1;
								$Racing::PlayerAmount++;
								%this.setControlObject(%brick.vehicle);
								return;
							}
						}
					}
				}
			} else {
				%camera = %this.Camera;
				%camera.setFlyMode();
				%camera.mode = "Observer";
				%this.setControlObject(%camera);
			}
		}
	}

	function Vehicle::finalExplosion(%this) {
		//%this.dump();
		echo("EXPLOSION CALLED");
		%this.spawnBrick.isUsed = 0;
		%client = %this.getControllingClient();
		if(isObject(%client)) {
			echo("CLIENT EXISTS:" SPC %client SPC %client.getClassName() SPC %client.getName());
			cancel(%client.statLoop);
			%camera = %client.Camera;
			%camera.setFlyMode();
			%camera.mode = "Observer";
			%client.setControlObject(%camera);
			%client.spawnBrick = "";
			if(isObject(%client.minigame)) {
				$Racing::PlayerAmount--;
			}
		} else {
			echo("CLIENT DOESN'T EXIST");
		}

		parent::finalExplosion(%this);
	}

	function serverCmdSuicide(%this) {
		if(isObject(%this.minigame)) {
			%this.player.finalExplosion();
			messageAll('',"<bitmap:base/client/ui/CI/skull.png>" SPC %this.name);
		}
	}

	function MinigameSO::Reset(%this,%client) {
		$Racing::HasStarted = 0;
		$Racing::PlayerAmount = 0;
		parent::Reset(%this,%client);

		//looks like i have to force respawn everyone?
		%this.respawnAll();
		messageAll('',"\c6The race will start in 12 seconds.");
		%this.schedule(10000,centerPrintAll,"\c0<font:Arial Bold:48>Ready?");
		%this.schedule(11000,centerPrintAll,"\c3<font:Arial Bold:48>Set?");
		%this.schedule(12000,startRace);
	}
};
activatePackage(GoKartPackage);