using HaruhiHeiretsuLib;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaruhiHeiretsuCLI
{
    public class CalculatorCommand : Command
    {
        private bool _floatToInt, _intToFloat;
        private string _firstOperand, _secondOperand;

        public CalculatorCommand() : base("calculator")
        {
            Options = new()
            {
                { "a|first-operand=", "First operand", a => _firstOperand = a },
                { "b|second-operand=", "Second operand", b => _secondOperand = b },
                { "float-to-int", "Calculate float to int", f => _floatToInt = true },
                { "int-to-float", "Calculate int to float", i => _intToFloat = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (_floatToInt)
            {
                float a = float.Parse(_firstOperand);
                CommandSet.Out.WriteLine($"Result: {Helpers.FloatToInt(a):X8}");
            }
            else if (_intToFloat)
            {
                int a = int.Parse(_firstOperand);
                CommandSet.Out.WriteLine($"Result: {Helpers.IntToFloat(a)}");
            }

            return 0;
        }
    }
}
