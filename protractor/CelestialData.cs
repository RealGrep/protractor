using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Protractor {

    public class CelestialData
    {
        public CelestialBody body;
        public string name;
        public double theta_angle;
        public double theta_time;
        public string theta_time_str;
        public double psi_angle;
        public double psi_time;
        public string psi_time_str;
        public double psi_angle_adjusted;
        public double psi_time_adjusted;
        public string psi_time_adjusted_str;
        public double deltaV;
        public double deltaV_time;
        public double adv_ejection_angle;
        public double closest_approach;
        public double closest_approach_time;

        public CelestialData(CelestialBody body)
        {
            this.body = body;
            this.name = body.name;
        }

        public void print()
        {
            KSPLog.print("Protractor: CelestialData: Name: " + name);
        }
    }
}

