## Changelog:
##### v2.0.0
##### MUCH NEEDED CODE CELANUP:
##### Minor compatibility breaks (should only break names intended for the event messages; hopefully).
##### Jumping to version 2.0.0, just for good practice. Highly recommended to update and just change the names.
##### - Grammar update: Ignitor -> Igniter
##### - Complete code clean up (slimer, faster, easier (too many bool variables and checks that kept piling up while I was adding functionality))
##### - Decoupler, Engines and RCS can now be triggered with a pre-delay, counting towards the Apogee after a launch. Usecase: Apogee Kick Stages
##### - Three Apogee Kick Stage cut-off modes (Full Burn (un-cut), Cut-Off at a target PE/AP, Cut-Off when the orbit is circular)
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
##### v1.2
##### OnScreen messaging system:
##### - Can be turned off
##### - Shows 3 pre messages before decoupling and igniting at -10s, -5s and -2s
##### - Shows a message at decoupling, igniting and fairing separation
##### v1.1.1
##### -inflight PAW change: Circuits are: connected/disconnected instead of active?: true/false
##### v1.1
##### -enable/disable button to disable funcionality instead of 0.0s/0km
##### v1.0
##### -initial release
