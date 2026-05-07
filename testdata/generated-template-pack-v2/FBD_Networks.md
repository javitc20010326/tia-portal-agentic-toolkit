# FBD Network Plan: AxisCarla

These networks are neutral FBD logic. To become real TIA Portal FBD XML, they need one exported FBD seed block from the same TIA Portal version.

## Blocks

- `AND3` for ready permissive.
- `AND3` for drive enable.
- `OR3` for stop priority.
- `NOT` blocks for fault, error, stop, and movement exclusion.

## Signal Flow

- `DriveReady`, `NOT DriveFault`, and `NOT Status.Error` feed `Status.Ready`.
- `Command.Enable`, `Status.Ready`, and `NOT Command.Stop` feed `EnableDrive`.
- `Command.Stop`, `DriveFault`, and `Status.Error` feed `StopDrive`.