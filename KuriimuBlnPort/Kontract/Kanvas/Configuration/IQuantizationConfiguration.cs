﻿using System;
using System.Drawing;
using Kontract.Kanvas.Quantization;

namespace Kontract.Kanvas.Configuration
{
    public interface IQuantizationConfiguration
    {
        IQuantizationConfiguration ConfigureOptions(Action<IQuantizationOptions> configure);

        IQuantizationConfiguration WithTaskCount(int taskCount);

        IQuantizer Build();

        IQuantizationConfiguration Clone();
    }
}
