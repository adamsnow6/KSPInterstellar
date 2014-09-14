﻿extern alias ORSv1_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_3::OpenResourceSystem;

namespace FNPlugin {
    class ReactorFuel {
        protected double _fuel_usege_per_mw;
        protected string _fuel_name;
        protected double _density;
        protected string _unit;

        public ReactorFuel(ConfigNode node) {
            _fuel_name = node.GetValue("FuelName");
            _fuel_usege_per_mw = Convert.ToDouble(node.GetValue("UsagePerMW"));
            _unit = node.GetValue("Unit");
            _density = PartResourceLibrary.Instance.GetDefinition(_fuel_name).density;
        }

        public double FuelUsePerMJ { get { return _fuel_usege_per_mw/_density; } }

        public string FuelName { get { return _fuel_name; } }

        public string Unit { get { return _unit; } }

        public double GetFuelUseForPower(double efficiency, double megajoules) {
            return _fuel_usege_per_mw * megajoules / _density / efficiency;
        }

    }
}
