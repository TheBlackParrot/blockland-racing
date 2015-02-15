function fxDTSBrick::getTopBrick(%this) {
	%upbrick = %this;
 
	while(isObject(%upbrick.getUpBrick(0))) {
		%upbrick = %upbrick.getUpBrick(0);
	}
 
	return %upbrick;
}

function serverCmdSetRequiredTrackBricks(%this) {
	if(%this.bl_id != getNumKeyID()) {
		return;
	}
	// eventually get around to setting a specific color for track bricks via track info
	for(%i=0;%i<BrickGroup_888888.getCount();%i++) {
		%brick = BrickGroup_888888.getObject(%i);
		if(%brick.colorID == 7) {
			if(!%brick.getNumUpBricks()) {
				%brick.isTrackBrick = 1;
				%count++;
				continue;
			} else {
				%selection = %brick.getTopBrick();
				if(%selection.isTrackBrick) {
					continue;
				} else {
					%selection.isTrackBrick = 1;
					%count++;
				}
			}
		}
	}
	messageClient(%this,'',"Set" SPC getTrackBrickCount() SPC "track bricks.");
	servercmdPreviewTrackBricks(%this);
}

function servercmdPreviewTrackBricks(%this) {
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

function WheeledVehicle::doBrickCheckLoop(%this) {
	cancel(%this.checkLoop);
	%this.checkLoop = %this.schedule(200,doBrickCheckLoop);
	if(!%this.laps) {
		%this.laps = 1;
	}

	echo("GOING");

	initContainerBoxSearch(%this.getPosition(), "20 20 20", $TypeMasks::FXBrickObjectType);
	while((%targetObject = containerSearchNext()) != 0 && isObject(%targetObject)) {
		if(%targetObject.isTrackBrick) {
			if(stripos(%this.hasTouched,%targetObject) == -1) {
				%this.hasTouched = %this.hasTouched @ %targetObject;
				%this.touchedCount++;
				talk("TOUCHED" SPC %targetObject SPC "[" @ %this.touchedCount @ "]");
				%targetObject.setColor(0);
				%targetObject.schedule(400,setColor,7);
			} else {
				echo("ALREADY TOUCHED" SPC %targetObject);
			}
		} else {
			echo("NOT TRACK BRICK" SPC %targetObject);
		}
	}

	initContainerBoxSearch(%this.getPosition(), "1 1 1", $TypeMasks::FXBrickObjectType);
	while((%targetObject = containerSearchNext()) != 0 && isObject(%targetObject)) {
		if(%targetObject.getName() $= "_race_lapbrick") {
			// fuzzy collision, oh boy
			// range though should be high enough in the first box search
			%required_amount = mCeil(getTrackBrickCount()/4);
			if(%this.touchedCount >= %required_amount) {
				%this.hasTouched = "";
				%this.touchedCount = 0;
				%this.laps++;
				talk("LAP" SPC %this.laps);
			}
		}
	}

	// really the only reason why this is here is to "predict" race positions and prevent you lame cheaters from cheating
}

//package GoKartPackage {
	// soon
//};
//activatePackage(GoKartPackage);