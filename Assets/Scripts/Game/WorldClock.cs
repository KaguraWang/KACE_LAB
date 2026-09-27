using System;
using System.Globalization;
using UnityEngine;

namespace ChemLab.Game {

/// <summary>
/// The single experiment-time gate behind the console command `world_speed`.
///
/// Every subsystem that advances chemistry, heat, metered transfer or the
/// fluid/solid solvers must scale its step through here, so that
/// `world_speed 0` freezes the experiment itself instead of only a UI clock.
/// Presentation stays on real frame time: the camera, menus, tags, the pet and
/// this console keep working while the experiment is frozen.
///
/// The rate is never clamped. Out-of-range, non-finite and unparsable values are
/// rejected by <see cref="TrySet"/> with an explanation, leaving the rate alone.
/// </summary>
public static class WorldClock {
    public const double MinimumRate = 0d;
    public const double MaximumRate = 3d;

    /// <summary>Current world speed; 1 is real time, 0 freezes the experiment.</summary>
    public static double Rate { get; private set; } = 1d;

    /// <summary>Scaled experiment seconds accumulated since the process started.</summary>
    public static double ScaledSeconds { get; private set; }

    public static bool Frozen => Rate <= 0d;

    /// <summary>Scales one real frame step into experiment time.</summary>
    public static float Scale(float realSeconds) {
        if (realSeconds <= 0f || Rate <= 0d) return 0f;
        double scaled = realSeconds * Rate;
        return scaled >= float.MaxValue ? float.MaxValue : (float)scaled;
    }

    /// <summary>The real frame step already scaled by the current world speed.</summary>
    public static float DeltaTime => Scale(Time.deltaTime);

    /// <summary>Records experiment time that a subsystem actually consumed.</summary>
    public static void AddElapsed(float scaledSeconds) {
        if (scaledSeconds <= 0f || float.IsNaN(scaledSeconds) || float.IsInfinity(scaledSeconds)) return;
        ScaledSeconds += scaledSeconds;
    }

    /// <summary>Parses the console argument without touching the current rate.</summary>
    public static bool TryParse(string argument, out double rate, out string error) {
        rate = Rate;
        error = null;
        string text = (argument ?? string.Empty).Trim();
        if (text.Length == 0) {
            error = "[ERR] world_speed needs a number in 0-3, for example: world_speed 1.5";
            return false;
        }
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) {
            error = "[ERR] world_speed: '" + text + "' is not a number";
            return false;
        }
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            error = "[ERR] world_speed: value must be finite";
            return false;
        }
        if (value < MinimumRate || value > MaximumRate) {
            error = "[ERR] world_speed: " + text + " is outside the supported range 0-3; command not executed";
            return false;
        }
        rate = value;
        return true;
    }

    /// <summary>Console entry point: rejects invalid input and reports the applied rate.</summary>
    public static bool TrySet(string argument, out string message) {
        if (!TryParse(argument, out double rate, out string error)) {
            message = error;
            return false;
        }
        Set(rate);
        // The note is informational only; the applied number is always the one
        // the operator typed, never a clamped substitute.
        if (rate == 1d) message = "world_speed 1  (experiment time follows real time)";
        else if (rate > 1d) message = "world_speed " + Format(rate) + "  (experiment runs " + Format(rate) + "x)";
        else if (rate > 0d) message = "world_speed " + Format(rate) + "  (experiment runs slower)";
        else message = "world_speed 0  (experiment frozen)";
        return true;
    }

    public static void Set(double rate) {
        if (double.IsNaN(rate) || double.IsInfinity(rate) || rate < MinimumRate || rate > MaximumRate)
            throw new ArgumentOutOfRangeException(nameof(rate), "World speed must be within 0-3.");
        Rate = rate;
    }

    /// <summary>Test-only: restores the default rate and clears the scaled clock.</summary>
    public static void ResetForTest() {
        Rate = 1d;
        ScaledSeconds = 0d;
    }

    static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
}
