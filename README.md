Fuzzman
=======

A simple Windows fuzzer, written in C#.

Task list
=========

* ~~Write target monitoring code to detect when the process becomes idle.~~ Done.
* ~~Allow for multiple sample files to be used in a single run.~~ Done.
* ~~Add parallel runs of the target.~~ Done.
* Profile the fuzzer -- consumes too much CPU for such a simple work.
* More flexible fuzzing algorithms, at least, some tweaking w/o rebuilbind the whole stuff.
* Some analysis of the test run results -- stack walks, stability, etc.
* Advanced cleanup after each run of the target program -- some save data in registry...
