# LAD Network Plan: AxisCarla

These networks are neutral ladder logic. To become real TIA Portal LAD XML, they need one exported LAD seed block from the same TIA Portal version.

## N001 Ready Permissive

`DriveReady` series `NOT DriveFault` series `NOT Status.Error` drives coil `Status.Ready`.

## N002 Drive Enable

`Command.Enable` series `Status.Ready` series `NOT Command.Stop` drives coil `EnableDrive`.

## N003 Positive Jog

`EnableDrive` series `Command.JogPositive` series `NOT PositiveLimit` series `NOT Command.Stop` drives coil `MovePositive`.

## N004 Negative Jog

`EnableDrive` series `Command.JogNegative` series `NOT NegativeLimit` series `NOT Command.Stop` drives coil `MoveNegative`.

## N005 Stop Output

Parallel branch of `Command.Stop`, `DriveFault`, and `Status.Error` drives coil `StopDrive`.