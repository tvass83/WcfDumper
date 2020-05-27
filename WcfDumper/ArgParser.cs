using System;
using System.Collections.Generic;
using System.Linq;

namespace WcfDumper
{
    public enum ErrorCode
    {
        Success = 0,
        NoArgument,
        WhiteSpace,
        MissingValueForSwitch,
        InvalidValueForSwitch,
        InvalidSwitch,
    }

    public static class ArgParser
    {
        public static List<string> Switches { get; set; } = new List<string>();
        public static Dictionary<string, string> SwitchesWithValues { get; set; } = new Dictionary<string, string>();
        public static List<string> OrdinaryArguments { get; set; } = new List<string>();

        public static ErrorCode Parse(string[] args, string[] expectedSwitches, string[] expectedSwitchesWithValues)
        {
            if (args.Length == 0)
            {
                return ErrorCode.NoArgument;
            }

            string lastSwitch = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(arg))
                {
                    return ErrorCode.WhiteSpace;
                }

                if (arg.StartsWith("-", StringComparison.InvariantCulture))
                {
                    if (lastSwitch != null)
                    {
                        return ErrorCode.InvalidValueForSwitch;
                    }

                    if (expectedSwitches.Contains(arg))
                    {
                        Switches.Add(arg);
                    }
                    else if (expectedSwitchesWithValues.Contains(arg))
                    {
                        lastSwitch = arg;
                    }
                    else
                    {
                        return ErrorCode.InvalidSwitch;
                    }
                }
                else if (lastSwitch != null)
                {
                    SwitchesWithValues[lastSwitch] = arg;
                    lastSwitch = null;
                }
                else
                {
                    OrdinaryArguments.Add(arg);
                }
            }

            if (lastSwitch != null)
            {
                return ErrorCode.MissingValueForSwitch;
            }

            return ErrorCode.Success;
        }
    }
}
