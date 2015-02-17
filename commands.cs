function serverCmdColor(%this,%colorR,%colorG,%colorB) {
	if(!isObject(%this.player)) {
		return;
	}
	if(getSimTime() - %this.lastColorChange <= 30000) {
		messageClient(%this,'',"You can only change your vehicle color once every 30 seconds.");
		return;
	}
	%player = %this.player;
	if(!mFloor(%colorR) && %colorG $= "" && %colorB $= "") {
		%colorR = strLwr(%colorR);
		switch$(%colorR) {
			case "maroon":
				%color = "0.5 0 0 1";
			case "red":
				%color = "1 0 0.1 1";
			case "green":
				%color = "0.2 0.8 0.3 1";
			case "blue":
				%color = "0.2 0.3 1 1";
			case "orange":
				%color = "1 0.4 0.2 1";
			case "yellow":
				%color = "1 0.9 0.1 1";
			case "lime":
				%color = "0.6 1 0.1 1";
			case "darkgreen":
				%color = "0 0.6 0.2 1";
			case "teal":
				%color = "0 0.5 0.6 1";
			case "cyan":
				%color = "0.1 0.9 1 1";
			case "skyblue":
				%color = "0.2 0.6 1 1";
			case "darkblue":
				%color = "0 0.5 0 1";
			case "indigo":
				%color = "0.25 0 0.8 1";
			case "purple":
				%color = "0.5 0.1 0.5 1";
			case "lavender":
				%color = "0.8 0.6 1 1";
			case "pink":
				%color = "1 0.1 0.9 1";
			case "brown":
				%color = "0.5 0.2 0 1";
			case "black":
				%color = "0.1 0.1 0.1 1";
			case "gray":
				%color = "0.6 0.6 0.6 1";
			case "white":
				%color = "1 1 1 1";
			default:
				%color = "1 1 1 1";
		}
		%player.setNodeColor("ALL",%color);
		%this.vehicleColor = %color;
		messageClient(%this,'',"You have changed your color to" SPC %colorR);
	} else {
		%player.setNodeColor("ALL",%colorR SPC %colorG SPC %colorB SPC "1");
		messageClient(%this,'',"You have changed your color to" SPC %colorR SPC %colorG SPC %colorB);
	}
}

function serverCmdListColors(%this) {
	messageClient(%this,'',"\c6maroon red green blue orange yellow lime darkgreen teal cyan skyblue darkblue indigo purple lavender pink brown black gray white");
}

function serverCmdType(%this,%type) {
	if(!isObject(%this.player) || %type $= "") {
		return;
	}
	%type = strLwr(%type);
	switch$(%type) {
		case "speedkart":
			%class = "";
		case "muscle":
			%class = %type;
		case "formula":
			%class = %type;
		case "hotrod":
			%class = %type;
		case "vintage":
			%class = %type;
		case "buggy":
			%class = %type;
		case "classic":
			%class = %type;
		case "classicgt":
			%class = %type;
		case "jeep":
			%class = %type;
		case "hyperion":
			%class = %type;
		case "64":
			%class = %type;
		case "blocko":
			%class = %type;
		case "7":
			%class = %type;
		case "lemans":
			%class = %type;
		default:
			%class = "classicgt";
	}
	%this.vehicleType = "SpeedKart" @ %class @ "Vehicle";
	messageClient(%this,'',"You have changed your vehicle type to" SPC %type);
}

function serverCmdListTypes(%this) {
	messageClient(%this,'',"\c6speedkart muscle formula hotrod vintage buggy classic classicgt jeep hyperion 64 blocko 7 lemans");
}

function serverCmdHelp(%this) {
	messageClient(%this,'',"\c3/color \c5[color] \c7-- \c6Sets your vehicle color. Use \c3/listcolors \c6for a list, or use RGB using numbers 0 - 1.");
	messageClient(%this,'',"\c3/type \c5[vehicle type] \c7-- \c6Sets your vehicle type. Use \c3/listtypes \c6for a list. \c5Takes effect on the next round.");
	messageClient(%this,'',"\c3/tracklist \c7-- \c6Lists the tracks on the server.");
}