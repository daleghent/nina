# Sequencer 2.0

## Problem

The current sequencer is rather static in its mode of operation. There is no possibility to have custom actions prior to or after running a sequence.
Only fixed defined patterns are supported. 
This works for a broader range of use cases, but won't ever cover the full range of requirements that users have for an automated sequence.

The sequencer should be much more versatile to allow for special equipment to be controlled as well as use cases like waiting for a sequence to start at a specific time of for a target reaching a specific altitude.


## Example Use Cases

-  Cooling a camera prior to starting an imaging run
-  Waiting for a sequence to start at a specific time
-  Waiting for a sequence to start until a target has reached a set altitude
-  Looping a sequence as long as the target is between a specific altitude range
-  Cover the scope in between sequences to mix light and dark frames
-  Park equipment at end of a sequence
-  Warm a camera
-  Ability to add certain events based on conditions. e.g. similar to the old autofocus triggers
-  Events need to be easily enhanced for things like star lost during guiding
-  More events will enable error recovery
-  Save custom building blocks for re-usability

## Things to consider

- Adding panels from framing into the new sequencer


## Data model

### Interfaces
- ISequenceItem
  - Run()

- ISequenceContainer : ISequenceItem
  - readonly ICollection<ISequenceBlock> { get; }
  - ICollection<ISequenceEvent> { get; } // loop through each event and check if should trigger?
  
  - Add(ISequenceItem)
  - Remove(ISequenceItem)

- ISequenceEvent
  - tbd

### Classes

- SequentialContainer : ISequenceContainer
  - Runs items one after the other

- ParallelContainer : ISequenceContainer
  - Runs items in parallel

- abstract ImagingContainer : ISequenceContainer
- SequentialImagingContainer : ImagingContainer
- LoopedImagingContainer : ImagingContainer // maybe unnecessary and merged with timed
- TimedImagingContainer : ImagingContainer
- AltitudeImagingContainer : ImagingContainer 

- DarkFrameContainer
- FlatFrameContainer



Example visions of users:

```
container
  +type sequential
  -camera cool to -15
  -camera gain 200
  -container
    # start event: time >23:00
    +type sequential
    +name my L sequence 1
    -center on ra X dec Y //slew + solve
    -filter switch L
    -guiding start
    -container
      +type loop
      +image type light
      # start event: time >23:00
      -take image 300s
      -dither
      # end event: time >01:30
  -container
    # start event: time >01:30
    +type sequential
    +name my rgb sequence 2
    -center on ra X2 dec Y2 //slew + solve
    -guiding start
    -container
      +type loop
      +image type light
      # start event: time >1:30
      -filter switch R
      -take image 300s
      -filter switch G
      -take image 300s
      -filter switch B
      -take image 300s
      -dither
      # end event: time >5:00
  -camera warm
  -mount park
```

```
Wait until {nautical dark}
Start camera cooler to -15 
Slew and center to target1 | Autofocus | Begin Guiding
Wait until {camera cooler < -15}
Image { L:45s; R:60s; G:60s; B:60s } 
    gain 111 offset 8
    loopingFilters
    dither {every 10m}
    until {moon > 15*}
Image {Ha:5m}
    gain 53 offset 10
    dither {every 2 frames}
    until {next target > 40* alt}
Slew and center to target2 | Autofocus | Begin Guiding
Image { Ha:5m:Gain 111 Offset 8; Oiii:10m:Gain 53 Offset 10; Sii:5m:Gain 111 Offset 8;} 
    loopingFilters
    dither {every 2 frames}
    until {target goals reached | nautical twilight}
Warm Camera | Park
```

```
Wait until {astro dark}
Image { L;R;G;B } 
    using optimum exposure calculator
        generating master dark sequence
    gain 111 offset 8
    loopingFilters
    dither {every 10m}
    until {moon < 5*}

Image { L;R;G;B } 
    using optimum exposure calculator --rerun
        generating master dark sequence
    gain 111 offset 8
    loopingFilters
    dither {every 10m}
    until {nautical twilight}
```