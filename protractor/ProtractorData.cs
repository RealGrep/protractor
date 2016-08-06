using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Protractor {

    public class ProtractorData
    {
        public List<CelestialBody>
            planets = null,
            moons = null,
            bodyList = null;
        public CelestialBody Sun = null;

        public Dictionary<string, CelestialData> celestials = new Dictionary<string, CelestialData>();

        public enum orbitbodytype { ksc, sun, planet, moon };


        public double closestApproachTime;
        public double maxthrustaccel;
        public double minthrustaccel;

        public ProtractorData()
        {
            initialize();
        }

        public void initialize()
        {
            Sun = Planetarium.fetch.Sun;
            getbodies();
            getplanets();
            getmoons();
            //Debug.Log("Protractor: ProtractorData initialized.");
            //print();
        }

        public void getmoons()  //gets a list of moons
        {
            // Not in flight, assume Kerbin
            if (!HighLogic.LoadedSceneIsFlight)
            {
                // Find Kerbin
                List<CelestialBody> all = new List<CelestialBody>(Sun.orbitingBodies);
                foreach (CelestialBody body in all)
                {
                    if (body.name == "Kerbin")
                    {
                        moons = new List<CelestialBody>(body.orbitingBodies);
                        break;
                    }
                }
            }
            // In interstellar space
            else if (FlightGlobals.ActiveVessel.mainBody == Sun)
            {
                if (moons != null)
                {
                    moons.Clear();
                }
                else
                {
                    moons = new List<CelestialBody>();
                }
            }
            // Orbiting a planet
            else if (FlightGlobals.ActiveVessel.mainBody.referenceBody == Planetarium.fetch.Sun)
            {
                moons = new List<CelestialBody>(FlightGlobals.ActiveVessel.mainBody.orbitingBodies);
            }
            // Orbiting a moon, gets all moons in planetary system
            else
            {
                moons = new List<CelestialBody>();
                List<CelestialBody> allmoons = new List<CelestialBody>(FlightGlobals.ActiveVessel.mainBody.referenceBody.orbitingBodies);
                foreach (CelestialBody moon in allmoons)
                {
                    if (FlightGlobals.ActiveVessel.mainBody != moon)
                    {
                        moons.Add(moon);
                    }
                }
            }
        }

        // Gets a list of celestial bodies orbiting the star
        public void getplanets()
        {
            if (planets != null)
            {
                planets.Clear();
            } else {
                planets = new List<CelestialBody>();
            }

            // Not in flight, assume we're on Kerbin and don't use vessel
            if (!HighLogic.LoadedSceneIsFlight)
            {
                foreach (CelestialBody body in Sun.orbitingBodies)
                {
                    if (body.name != "Kerbin")
                    {
                        planets.Add(body);
                    }
                }
            }
            // In flight, so use current vessel
            else
            {
                foreach (CelestialBody body in bodyList)
                {
                    if (body.referenceBody == Planetarium.fetch.Sun)
                    {
                        if (body == Sun || body == FlightGlobals.ActiveVessel.mainBody || body == FlightGlobals.ActiveVessel.mainBody.referenceBody)
                        {
                            continue;
                        }
                        planets.Add(body);
                    }
                }
            }
        }

        // Gets a list of all celestial bodies in the system
        public void getbodies()
        {
            bodyList = null;
            bodyList = new List<CelestialBody>(FlightGlobals.Bodies);
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (!celestials.ContainsKey(body.name))
                {
                    celestials.Add(body.name, new CelestialData(body));
                }
            }
        }

        public orbitbodytype getorbitbodytype()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return orbitbodytype.ksc;
            }
            else if (FlightGlobals.ActiveVessel.mainBody == Sun)
            {
                return orbitbodytype.sun;
            }
            else if (FlightGlobals.ActiveVessel.mainBody.referenceBody != Sun)
            {
                return orbitbodytype.moon;
            }
            else
            {
                return orbitbodytype.planet;
            }
        }

        public void print()
        {
            foreach (KeyValuePair<string, CelestialData> data in celestials)
            {
                data.Value.print();
            }
        }

    }
}

