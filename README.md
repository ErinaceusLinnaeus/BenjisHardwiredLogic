# BenjisHardwiredLogic
------------------------------------------------------------------------
## Version:
##### v1.0
------------------------------------------------------------------------
## Installation:
##### Simply drag the folder BenjisHardwiredLogic into your GameData folder.
------------------------------------------------------------------------
## Description:
##### A ksp mod to "hardwire"-logic into rocket designs.
##### (At this point: decoupler-staging, fairing-separation and engine/rcs-ignition).

##### Decouplers and also Fairingbases can have a delay assigned (up to 1200.0s (That's 20min)).
The same for Engines (solid and "liquid") and RCS thrusters.
Procedural Fairings (only RO, not the squad basegame thingies) can have a height between 0 and 140 km assigned.

##### Every assigned 0 will be ignored, is thus unconfigured/unset.

##### Every other value will start counting down once the launchclamps are released.
When reaching 0 the decoupler will automatically decouple, the engine light up and the RCS become active.
Fairings automatically separate at the set height (above sealevel).

##### This will save a lot of space in the StagingList because the whole launcher can be put into 3 stages.
But you can also organize decouplers and enginges the way you usually do. The upside is that you can always stage, decouple and separate fairings ahead of countdown. This mod does not prevent that.

------------------------------------------------------------------------
## Example (of your standard launcher):

#### Stages 0-4
            un-automated payload-stuff, whatever you wanna put into space...
#### Stage 5
            2x radialDecouplers connected to the solidMotors (delayed for 30.5s)
            2ndStageEngine (2min burntime) (delayed for 63.8s)
            2ndStageDecoupler between 1st and 2nd stage (delayed for 64.8s)
            payloadDecoupler between 2nd stage and payload (delayed for 184s)
            procFairing (set for 90km)
##### Stage 6
            launchClamps
            2x solidMotors (30s burntime)
#### Stage 7
            1st stage engine (65s burntime)
         
So, let's look at what will happen, shall we?

First spacebar fires the main engine.
Once that is spooled up, spacebar fires the solidMotors and releases the launchClamps. Now the countdowns will start.
At 30.5s the solidMotors will decouple
At 63.8s the 2nd stage will fire.
At 64.8s the 1st stage will be decoupled, hopefully the 2nd stage is spooled up by now.
At 90km the fairings will separate.
2 minutes 4 seconds after lift off: The payload will be decoupled from the 2nd stage. 
