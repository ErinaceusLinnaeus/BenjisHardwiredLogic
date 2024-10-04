## Changelog:
##### 2.2.0
##### Addition:
##### - Benji's Ballistic Throw: 'Simple automated guidance computer for those ballistic shots (downrange contracts)
##### Change:
##### - Excluding the Launch Clamps module from stock. Only makes sense for thos RO engine spool up times
##### 2.1.0
##### Addition:
##### - Launch Clamps have a timer now
#####   (Because of that, the flight timer starts at engine light. Keep that in mind and add that to you're other timers)
##### Fixes:
##### - Some fields were visible even when inactive
##### - Messaging System gave up after loading
##### Errors brought to my attention by gotmachine (MANY THANKS)
##### - Fixing [KSPCF:MemoryLeaks] by removing functions from GameEvents
##### - Cleaning up the mess that every field was persistent, because I was too lazy to think about what really needs to be
##### - Change a terrible idea (await) into a coroutine
##### - Changed some strings from NULL to being empty
##### 2.0.1
##### - Include a .version file to let ckan know exactly about downwards compatibility
##### - Corrected an error in the pdf (the Fairing separator doesn't have a delay mode, stupid copy-paste mistake)
##### v2.0.0
##### MUCH NEEDED CODE CELANUP:
##### Jumping to version 2.0.0.
##### BREAKS COMPATIBILITY: Vessel loading will mark old vessels as missing Modules and most Data will be lost and needs to be reconfigured.
##### - Complete code clean up (slimer, faster, easier, more reliable ...(too many bools and checks that kept piling up while I was adding functionality)
##### - Decoupler, Engines and RCS can now be triggered with a pre-delay, counting towards the Apogee after a launch. Usecase: Apogee Kick Stages
##### - Three Apogee Kick Stage cut-off modes (Full Burn (un-cut), Cut-Off at a set PE/AP, Cut-Off when the orbit is (as close as possible to) circular)
##### - An Engine can also automatically cut-off when reaching a set Apside
##### - Event Messaging can now also be toggle in Flight
##### v1.2.2
##### MORE MESSAGING OPTIONS:
##### - Added a rare 4th Stage and a Spin-Motor Option for Igniting and Seperating
##### - Grammar update: Editor: Igniting -> Ignite
##### - Grammar update: Editor: Decoupling -> Decouple
##### - Grammar update: Messaging: Jettison -> Jettisoning
##### - RCS with its own correct grammar
##### v1.2.1
##### CHANGELOG & README:
##### - Added to the release zip
##### OnScreen messaging system:
##### - All messages are now schown in the upper center
##### - Messages can be turned off now, forgot to implement that earlier
##### v1.2.0
##### OnScreen messaging system:
##### - Can be turned off
##### - Shows 3 pre messages before decoupling and igniting at -10s, -5s and -2s
##### - Shows a message at decoupling, igniting and fairing separation
##### v1.1.1
##### -inflight PAW change: Circuits are: connected/disconnected instead of active?: true/false
##### v1.1.0
##### -enable/disable button to disable funcionality instead of 0.0s/0km
##### v1.0.0
##### -initial release
