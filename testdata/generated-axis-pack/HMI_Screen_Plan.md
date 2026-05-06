# HMI Screen Plan: AxisCarla

Project: `Proyecto3_Carla`
TIA Portal target: `V16`
Profile: `self`

Keep the screen practical for one engineer using it during tests and iteration.

## Screen

Name: `AxisCarla_Overview`

Main zones:

- Header with axis name, current state, and fault indicator.
- Command area with enable, jog positive, jog negative, move absolute, stop, and reset.
- Setpoint area with target position, velocity, acceleration, and deceleration.
- Feedback area with actual position, at-target state, busy/done/error flags, and state text.
- Diagnostics area with error code, drive-ready feedback, limit switches, and last operator action.

## HMI Tags

- `DB_AxisCarla_Data.Command.Enable`
- `DB_AxisCarla_Data.Command.Reset`
- `DB_AxisCarla_Data.Command.JogPositive`
- `DB_AxisCarla_Data.Command.JogNegative`
- `DB_AxisCarla_Data.Command.MoveAbsolute`
- `DB_AxisCarla_Data.Command.Stop`
- `DB_AxisCarla_Data.Command.TargetPosition`
- `DB_AxisCarla_Data.Command.Velocity`
- `DB_AxisCarla_Data.Status.Ready`
- `DB_AxisCarla_Data.Status.Busy`
- `DB_AxisCarla_Data.Status.Done`
- `DB_AxisCarla_Data.Status.Error`
- `DB_AxisCarla_Data.Status.ErrorCode`
- `DB_AxisCarla_Data.Status.ActualPosition`
- `DB_AxisCarla_Data.Status.StateText`

## Operator Behavior

- Use momentary buttons for jog, stop, reset, and move absolute.
- Use a maintained toggle or explicit on/off pair for enable.
- Disable move commands when `Status.Ready = FALSE`.
- Make stop visible and reachable without changing screen.
- Show fault state with error code and reset action, but do not hide diagnostics after reset.

## Alarms

- Axis drive fault: `Status.Error = TRUE` and `Status.ErrorCode = 16#0100`.
- Positive limit reached during positive command: `Status.ErrorCode = 16#0201`.
- Negative limit reached during negative command: `Status.ErrorCode = 16#0202`.
- Target unreachable or blocked by limit: `Status.ErrorCode = 16#0300`.

## Validation

- Test all buttons with outputs disconnected or in simulation first.
- Confirm jog commands are momentary.
- Confirm stop has priority over move and jog commands.
- Confirm limit-switch behavior before connecting real motion hardware.