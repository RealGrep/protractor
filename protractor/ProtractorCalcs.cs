using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;
using KSP;
using KSP.UI;
using KSP.UI.Util;
using KSP.IO;
using KSP.UI.Screens;
using KSP.UI.Rendering;




namespace Protractor {

    public class ProtractorCalcs
    {
        private ProtractorData pdata;

        CelestialBody focusbody = null;

        public ProtractorCalcs(ProtractorData pdata)
        {
            this.pdata = pdata;
        }

        public void update(Dictionary<string, CelestialData> celestials)
        {
            calcPlanetData(celestials);
            calcMoonData(celestials);
        }

        public void calcPlanetData(Dictionary<string, CelestialData> celestials)
        {
            //foreach (KeyValuePair<string, CelestialData> pair in celestials)
            foreach (CelestialBody body in pdata.planets)
            {
                CelestialData bodydata = pdata.celestials[body.name];
                if (!(body.Equals(focusbody)) && (focusbody != null))
                {
                    continue; //focus body defined and it isn't this one
                }

                bodydata.name = body.name;

                // Calculate theta
                double data;
                if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon) //get the data
                {
                    data = (CurrentPhase(body) - OberthDesiredPhase(body) + 360) % 360;
                }
                else
                {
                    data = (CurrentPhase(body) - DesiredPhase(body) + 360) % 360;
                }
                bodydata.theta_angle = data;

                double delta_theta;
                if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
                {
                    CelestialBody o = pdata.vessel.orbit.referenceBody.orbit.referenceBody;
                    delta_theta = (360 / o.orbit.period) - (360 / body.orbit.period);
                }
                else if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.planet)
                {
                    CelestialBody o = pdata.vessel.orbit.referenceBody;
                    delta_theta = (360 / o.orbit.period) - (360 / body.orbit.period);
                }
                else
                {
                    delta_theta = (360 / pdata.vessel.orbit.period) - (360 / body.orbit.period);
                }

                if (delta_theta > 0)
                {
                    data /= delta_theta;
                }
                else
                {
                    data = Math.Abs((360 - data) / (delta_theta));
                }
                bodydata.theta_time = data;
                bodydata.theta_time_str = TimeToDHMS(data);

                // Calculate psi
                if (pdata.getorbitbodytype() != ProtractorData.orbitbodytype.planet)
                {
                    bodydata.psi_time = -1;
                    bodydata.psi_time_str = "";
                    bodydata.psi_angle = 0.0;
                    bodydata.psi_time_adjusted = -1;
                    bodydata.psi_time_adjusted_str = "";
                    bodydata.psi_angle_adjusted = 0.0;
                } else {
                    bodydata.psi_angle = (CalculateDesiredEjectionAngle(pdata.vessel.mainBody, body) - CurrentEjectAngle(null) + 360) % 360;
                    if (tmr() > 0)
                    {
                        bodydata.psi_angle_adjusted = (AdjustEjectAngle(pdata.vessel.mainBody, body) - CurrentEjectAngle(null) + 360) % 360;
                    } else
                    {
                        bodydata.psi_angle_adjusted = -1;
                    }

                    bodydata.psi_time = bodydata.psi_angle / (360 / pdata.vessel.orbit.period);
                    bodydata.psi_time_str = TimeToDHMS(bodydata.psi_time);
                    if (bodydata.psi_angle_adjusted == -1)
                    {
                        bodydata.psi_time_adjusted = -1;
                        bodydata.psi_time_adjusted_str = "0 TMR";
                    } else
                    {
                        bodydata.psi_time_adjusted = bodydata.psi_angle_adjusted / (360 / pdata.vessel.orbit.period);
                        bodydata.psi_time_adjusted_str = TimeToDHMS(bodydata.psi_time_adjusted);
                    }
                }

                // Calculate Delta-V
                if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
                {
                    bodydata.deltaV = 0.0;
                    bodydata.deltaV_time = 0.0;
                }
                else
                {
                    bodydata.deltaV = CalculateDeltaV(body);

                    double thrust = calcThrust();
                    if (pdata.vessel.ctrlState.mainThrottle != 0)
                    {
                        thrust *= pdata.vessel.ctrlState.mainThrottle;
                    }
                    bodydata.deltaV_time = calcBurnTime(bodydata.deltaV, pdata.vessel.GetTotalMass(), thrust);
                }

                // Calculate Closest Approach
                double distance = getclosestapproach(body);
                bodydata.closest_approach = distance;

                // Advanced
                if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
                {
                    bodydata.adv_ejection_angle = (CalculateDesiredEjectionAngle(pdata.vessel.mainBody.orbit.referenceBody, body) + 180 - CurrentEjectAngle(pdata.vessel.mainBody) + 360) % 360;
                } else {
                    bodydata.adv_ejection_angle = -1;
                }
            }
        }

        public void calcMoonData(Dictionary<string, CelestialData> celestials)
        {
            foreach (CelestialBody body in pdata.moons)
            {
                CelestialData bodydata = celestials[body.name];
                if (!(body.Equals (focusbody)) && (focusbody != null))
                {
                    continue;
                }

                bodydata.name = body.name;

                // Calculate theta
                double data = (CurrentPhase(body) - DesiredPhase(body) + 360) % 360;

                bodydata.theta_angle = data;

                double delta_theta;
                if (pdata.vessel.Landed && pdata.getorbitbodytype() == ProtractorData.orbitbodytype.planet)  //ship is landed on a planet, use rotation of the planet
                {
                    //double ves_vel = vessel.horizontalSrfSpeed;
                    double ves_vel = pdata.vessel.orbit.getOrbitalSpeedAtPos(pdata.vessel.CoM);
                    double radius = pdata.vessel.altitude + pdata.vessel.mainBody.Radius;
                    double circumference = Math.PI * 2 * radius;
                    double rot = circumference / ves_vel;
                    delta_theta = (360 / rot)-(360 / body.orbit.period);
                }
                else if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.planet)   //ship orbiting a planet, but is not landed
                {
                    delta_theta = (360 / pdata.vessel.orbit.period) - (360 / body.orbit.period);
                }
                else     //ship orbiting a moon
                {
                    CelestialBody o = pdata.vessel.mainBody;
                    delta_theta = (360 / o.orbit.period) - (360 / body.orbit.period);
                }

                //double delta_theta = (360 / vessel.orbit.referenceBody.orbit.period) - (360 / moon.orbit.period);   //FIX THIS - COMPARE ROTATION OF PLANET TO MOON OR USE TWO PLANET MODEL FOR TWO MOONS
                if (delta_theta > 0)
                {
                    data/= delta_theta;
                }
                else
                {
                    data = Math.Abs((360 - data) / (delta_theta));
                }

                bodydata.theta_time = data;
                bodydata.theta_time_str = TimeToDHMS(data);

                // Calculate Psi
                if (pdata.getorbitbodytype() == ProtractorData.orbitbodytype.planet) //vessel and moon share planet
                {
                    bodydata.psi_time = 0.0;
                    bodydata.psi_time_str = "";
                    bodydata.psi_angle = 0.0;
                    bodydata.psi_time_adjusted = 0.0;
                    bodydata.psi_time_adjusted_str = "";
                    bodydata.psi_angle_adjusted = 0.0;
                }
                else //vessel orbiting moon
                {
                    bodydata.psi_angle = (CalculateDesiredEjectionAngle(pdata.vessel.mainBody, body) - CurrentEjectAngle(null) + 360) % 360;
                    if (tmr() > 0)
                    {
                        bodydata.psi_angle_adjusted = (AdjustEjectAngle(pdata.vessel.mainBody, body) - CurrentEjectAngle(null) + 360) % 360;
                    } else
                    {
                        bodydata.psi_angle_adjusted = -1;
                    }

                    bodydata.psi_time = bodydata.psi_angle / (360 / pdata.vessel.orbit.period);
                    bodydata.psi_time_str = TimeToDHMS(bodydata.psi_time);
                    if (bodydata.psi_angle_adjusted == -1)
                    {
                        bodydata.psi_time_adjusted = -1;
                        bodydata.psi_time_adjusted_str = "0 TMR";
                    } else
                    {
                        bodydata.psi_time_adjusted = bodydata.psi_angle_adjusted / (360 / pdata.vessel.orbit.period);
                        bodydata.psi_time_adjusted_str = TimeToDHMS(bodydata.psi_time_adjusted);
                    }
                }

                // Calculate Delta-V
                bodydata.deltaV = CalculateDeltaV(body);

                double thrust = calcThrust();
                if (pdata.vessel.ctrlState.mainThrottle != 0)
                {
                    thrust *= pdata.vessel.ctrlState.mainThrottle;
                }
                bodydata.deltaV_time = calcBurnTime(bodydata.deltaV, pdata.vessel.GetTotalMass(), thrust);

                // Calculate Closest Approach
                double distance = getclosestapproach(body);
                bodydata.closest_approach = distance;
            }
        }


        /*
         * Mathy stuff.
         */
        public double getclosestapproach(CelestialBody target)
        {
            Orbit closestorbit = new Orbit();
            closestorbit = getclosestorbit(target);
            if (closestorbit.referenceBody == target)
            {
                pdata.closestApproachTime = closestorbit.StartUT + closestorbit.timeToPe;
                return closestorbit.PeA;
            }
            else if (closestorbit.referenceBody == target.referenceBody)
            {
                return mindistance(target, closestorbit.StartUT, closestorbit.period / 10, closestorbit) - target.Radius;
            }
            else
            {
                return mindistance(target, Planetarium.GetUniversalTime(), closestorbit.period / 10, closestorbit) - target.Radius;
            }
        }

        public Orbit getclosestorbit(CelestialBody target)
        {
            Orbit checkorbit = pdata.vessel.orbit;
            int orbitcount = 0;

            // Search for target
            while (checkorbit.nextPatch != null && checkorbit.patchEndTransition != Orbit.PatchTransitionType.FINAL &&
                orbitcount < 3)
            {
                checkorbit = checkorbit.nextPatch;
                orbitcount += 1;
                if (checkorbit.referenceBody == target)
                {
                    return checkorbit;
                }

            }
            checkorbit = pdata.vessel.orbit;
            orbitcount = 0;

            // Search for target's referencebody
            while (checkorbit.nextPatch != null && checkorbit.patchEndTransition != Orbit.PatchTransitionType.FINAL &&
                orbitcount < 3)
            {
                checkorbit = checkorbit.nextPatch;
                orbitcount += 1;
                if (checkorbit.referenceBody == target.orbit.referenceBody)
                {
                    return checkorbit;
                }
            }

            return pdata.vessel.orbit;
        }

        public double mindistance(CelestialBody target, double time, double dt, Orbit vesselorbit)
        {
            double[] dist_at_int = new double[11];
            for (int i = 0; i <= 10; i++)
            {
                double step = time + i * dt;
                dist_at_int[i] = (target.getPositionAtUT(step) - vesselorbit.getPositionAtUT(step)).magnitude;
            }
            double mindist = dist_at_int.Min();
            double maxdist = dist_at_int.Max();
            int minindex = Array.IndexOf(dist_at_int, mindist);

            pdata.closestApproachTime = time + minindex * dt;

            if ((maxdist - mindist) / maxdist >= 0.00001)
            {
                mindist = mindistance(target, time + ((minindex - 1) * dt), dt / 5, vesselorbit);
            }

            return mindist;
        }

        // Projects two vectors to 2D plane and returns angle between them
        public double Angle2d(Vector3d vector1, Vector3d vector2)
        {
            Vector3d v1 = Vector3d.Project(new Vector3d(vector1.x, 0, vector1.z), vector1);
            Vector3d v2 = Vector3d.Project(new Vector3d(vector2.x, 0, vector2.z), vector2);
            return Vector3d.Angle(v1, v2);
        }

        // Calculates phase angle between the current body and target body
        public double CurrentPhase(CelestialBody target)
        {
            Vector3d vecthis = new Vector3d();
            Vector3d vectarget = new Vector3d();
            vectarget = target.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());

            // Vessel orbits a moon, target is a planet (going down)
            if (target.referenceBody == pdata.Sun && pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon)
            {
                vecthis = pdata.vessel.mainBody.referenceBody.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());
            }
            //vessel and target orbit same body (going parallel)
            else if (pdata.vessel.mainBody == target.referenceBody)
            {
                vecthis = pdata.vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()); //going up
            }
            else
            {
                vecthis = pdata.vessel.mainBody.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());
            }

            double phase = Angle2d(vecthis, vectarget);

            vecthis = Quaternion.AngleAxis(90, Vector3d.forward) * vecthis;

            if (Angle2d(vecthis, vectarget) > 90)
            {
                phase = 360 - phase;
            }

            return (phase + 360) % 360;
        }

        // Calculates angle between vessel's position and prograde of orbited body
        public double CurrentEjectAngle(CelestialBody check)
        {
            Vector3d vesselvec = new Vector3d();
            vesselvec = check == null ?
                pdata.vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()) :
                check.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());

            Vector3d bodyvec = new Vector3d();
            bodyvec = pdata.getorbitbodytype() == ProtractorData.orbitbodytype.moon &&
                check != null ?
                bodyvec = pdata.vessel.mainBody.orbit.referenceBody.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()) :
                pdata.vessel.mainBody.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()); //get planet's position relative to universe

            double eject = Angle2d(vesselvec, Quaternion.AngleAxis(90, Vector3d.forward) * bodyvec);

            if (Angle2d(vesselvec, Quaternion.AngleAxis(180, Vector3d.forward) * bodyvec) > Angle2d(vesselvec, bodyvec))
            {
                eject = 360 - eject; //use cross vector to determine up or down
            }

            return eject;
        }

        // Calculates phase angle for rendezvous between two bodies orbiting same parent
        public double DesiredPhase(CelestialBody dest)
        {
            CelestialBody orig = pdata.vessel.mainBody;
            double o_alt =
                (pdata.vessel.mainBody == dest.orbit.referenceBody) ?
                (pdata.vessel.mainBody.GetAltitude(pdata.vessel.findWorldCenterOfMass())) + dest.referenceBody.Radius : //going "up" from sun -> planet or planet -> moon
                calcmeanalt(orig);  //going lateral from moon -> moon or planet -> planet
            double d_alt = calcmeanalt(dest);
            double u = dest.referenceBody.gravParameter;
            double th = Math.PI * Math.Sqrt(Math.Pow(o_alt + d_alt, 3) / (8 * u));
            double phase = (180 - Math.Sqrt(u / d_alt) * (th / d_alt) * (180 / Math.PI));
            while (phase < 0)
            {
                phase += 360;
            }
            return phase % 360;
        }

        // For going from a moon to another planet exploiting oberth effect
        public double OberthDesiredPhase(CelestialBody dest)
        {
            CelestialBody moon = pdata.vessel.mainBody;
            CelestialBody planet = pdata.vessel.mainBody.referenceBody;
            double planetalt = calcmeanalt(planet);
            double destalt = calcmeanalt(dest);
            double moonalt = calcmeanalt(moon);
            double usun = dest.referenceBody.gravParameter;
            double uplanet = planet.gravParameter;
            double oberthalt = (planet.Radius + planet.atmosphereDepth) * 1.05;

            double th1 = Math.PI * Math.Sqrt(Math.Pow(moonalt + oberthalt, 3) / (8 * uplanet));
            double th2 = Math.PI * Math.Sqrt(Math.Pow(planetalt + destalt, 3) / (8 * usun));

            double phase = (180 - Math.Sqrt(usun / destalt) * ((th1 + th2) / destalt) * (180 / Math.PI));
            while (phase < 0)
            {
                phase += 360;
            }
            return phase % 360;
        }

        // Calculates ejection v to reach destination
        public double CalculateDeltaV(CelestialBody dest)
        {
            if (pdata.vessel.mainBody == dest.orbit.referenceBody)
            {
                double radius = dest.referenceBody.Radius;
                double u = dest.referenceBody.gravParameter;
                double d_alt = calcmeanalt(dest);
                double alt = (pdata.vessel.mainBody.GetAltitude(pdata.vessel.findWorldCenterOfMass())) + radius;
                double v = Math.Sqrt(u / alt) * (Math.Sqrt((2 * d_alt) / (alt + d_alt)) - 1);
                return Math.Abs((Math.Sqrt(u / alt) + v) - pdata.vessel.orbit.GetVel().magnitude);
            }
            else
            {
                CelestialBody orig = pdata.vessel.mainBody;
                double d_alt = calcmeanalt(dest);
                double o_radius = orig.Radius;
                double u = orig.referenceBody.gravParameter;
                double o_mu = orig.gravParameter;
                double o_soi = orig.sphereOfInfluence;
                double o_alt = calcmeanalt(orig);
                double exitalt = o_alt + o_soi;
                double v2 = Math.Sqrt(u / exitalt) * (Math.Sqrt((2 * d_alt) / (exitalt + d_alt)) - 1);
                double r = o_radius + (pdata.vessel.mainBody.GetAltitude(pdata.vessel.findWorldCenterOfMass()));
                double v = Math.Sqrt((r * (o_soi * v2 * v2 - 2 * o_mu) + 2 * o_soi * o_mu) / (r * o_soi));
                return Math.Abs(v - pdata.vessel.orbit.GetVel().magnitude);
            }
        }

        // Calculates ejection angle to reach destination body from origin body
        public double CalculateDesiredEjectionAngle(CelestialBody orig, CelestialBody dest)
        {
            double o_alt = calcmeanalt(orig);
            double d_alt = calcmeanalt(dest);
            double o_soi = orig.sphereOfInfluence;
            double o_radius = orig.Radius;
            double o_mu = orig.gravParameter;
            double u = orig.referenceBody.gravParameter;
            double exitalt = o_alt + o_soi;
            double v2 = Math.Sqrt(u / exitalt) * (Math.Sqrt((2 * d_alt) / (exitalt + d_alt)) - 1);
            double r = o_radius + (pdata.vessel.mainBody.GetAltitude(pdata.vessel.findWorldCenterOfMass()));
            double v = Math.Sqrt((r * (o_soi * v2 * v2 - 2 * o_mu) + 2 * o_soi * o_mu) / (r * o_soi));
            double eta = Math.Abs(v * v / 2 - o_mu / r);
            double h = r * v;
            double e = Math.Sqrt(1 + ((2 * eta * h * h) / (o_mu * o_mu)));
            double eject = (180 - (Math.Acos(1 / e) * (180 / Math.PI))) % 360;

            eject = o_alt > d_alt ? 180 - eject : 360 - eject;

            return pdata.vessel.orbit.inclination > 90 && !(pdata.vessel.Landed) ? 360 - eject : eject;
        }

        // Calculates eject angle for moon -> planet in preparation for planet -> planet transfer
        public double MoonAngle()
        {
            CelestialBody orig = pdata.vessel.mainBody;
            double o_alt = calcmeanalt(orig);
            double d_alt = (pdata.vessel.mainBody.orbit.referenceBody.Radius + pdata.vessel.mainBody.orbit.referenceBody.atmosphereDepth) * 1.05;
            double o_soi = orig.sphereOfInfluence;
            double o_radius = orig.Radius;
            double o_mu = orig.gravParameter;
            double u = orig.referenceBody.gravParameter;
            double exitalt = o_alt + o_soi;
            double v2 = Math.Sqrt(u / exitalt) * (Math.Sqrt((2 * d_alt) / (exitalt + d_alt)) - 1);
            double r = o_radius + (pdata.vessel.mainBody.GetAltitude(pdata.vessel.findWorldCenterOfMass()));
            double v = Math.Sqrt((r * (o_soi * v2 * v2 - 2 * o_mu) + 2 * o_soi * o_mu) / (r * o_soi));
            double eta = Math.Abs(v * v / 2 - o_mu / r);
            double h = r * v;
            double e = Math.Sqrt(1 + ((2 * eta * h * h) / (o_mu * o_mu)));
            double eject = (180 - (Math.Acos(1 / e) * (180 / Math.PI))) % 360;

            eject = o_alt > d_alt ? 180 - eject : 360 - eject;

            return pdata.vessel.orbit.inclination > 90 && !(pdata.vessel.Landed) ? 360 - eject : eject;
        }

        public double AdjustEjectAngle(CelestialBody orig, CelestialBody dest)
        {
            double ang = CalculateDesiredEjectionAngle(orig, dest);
            double adj = 0;
            double time = (0.2 / 0.3) * burnlength(CalculateDeltaV(dest));
            adj = ang - (360 * (time / pdata.vessel.orbit.period));
            adj = adj < 0 ? adj += 360 : adj;
            return adj;
        }

        public double calcmeanalt(CelestialBody body)
        {
            return body.orbit.semiMajorAxis * (1 + body.orbit.eccentricity * body.orbit.eccentricity / 2);
        }



		// Thrust to Mass Ratio, I guess
        public double tmr()
        {
            Vector3d forward = pdata.vessel.transform.up;
            double totalmass, thrustmax, thrustmin;
            totalmass = thrustmax = thrustmin = 0;
            foreach (Part p in pdata.vessel.parts)
            {
				if( p != null && p.physicalSignificance != Part.PhysicalSignificance.NONE )
                {
                    totalmass += p.mass;

                    foreach (PartResource pr in p.Resources)
                    {
						if (pr != null )
							totalmass += pr.amount * pr.info.density;
                    }
                }



				if( ( p.State == PartStates.ACTIVE ) || ( StageManager.CurrentStage > StageManager.LastStage && p.inverseStage == StageManager.LastStage ) )
                {
                    if (p.Modules.Contains("ModuleEngines"))
                    {
                        foreach (PartModule pm in p.Modules)
                        {
                            if (pm != null && pm is ModuleEngines && pm.isActiveAndEnabled)
                            {
                                ModuleEngines me = (ModuleEngines)pm;
                                //double amountforward = Vector3d.Dot(me.thrustTransform.rotation * me.thrust, forward);
                                if (me.isOperational && !me.getFlameoutState)
                                {
                                    //double isp = me.atmosphereCurve.Evaluate((float)(vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres)) * me.g;
                                    //thrustmax += isp * me.maxFuelFlow;
                                    //thrustmin += isp * me.minFuelFlow;
                                    thrustmax += me.maxThrust;
                                    thrustmin += me.minThrust;
                                }
                            }
                        }
                    }
                    else if (p.Modules.Contains("ModuleEnginesFX"))
                    {
                        foreach (PartModule pm in p.Modules)
                        {
                            if (pm != null && pm is ModuleEnginesFX && pm.isActiveAndEnabled)
                            {
                                ModuleEnginesFX me = (ModuleEnginesFX)pm;
                                //double amountforward = Vector3d.Dot(me.thrustTransform.rotation * me.thrust, forward);
                                if (me.isOperational && !me.getFlameoutState)
                                {
                                    //double isp = me.atmosphereCurve.Evaluate((float)(vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres)) * me.g;
                                    //thrustmax += isp * me.maxFuelFlow;
                                    //thrustmin += isp * me.minFuelFlow;
                                    thrustmax += me.maxThrust;
                                    thrustmin += me.minThrust;
                                }
                            }
                        }
                    }
                }
            }
            pdata.maxthrustaccel = thrustmax / totalmass;
            pdata.minthrustaccel = thrustmin / totalmass;
            return thrustmax / totalmass;
        }

        // TODO: Account for thrust vector that is offset from CoM
        double calcBurnTime(double deltaV, double initialMass, double thrust)
        {
            return initialMass * deltaV / thrust;
        }

        double calcThrust()
        {
            double totalThrust = 0.0;
            Vessel vessel = FlightGlobals.fetch.activeVessel;

            foreach (Part part in vessel.parts)
            {
                if (part.Modules.Contains("ModuleEngines"))
                {
                    foreach (PartModule module in part.Modules)
                    {
                        if (module != null && module is ModuleEngines && module.isActiveAndEnabled)
                        {
                            ModuleEngines engine = (ModuleEngines)module;
                            if (engine.isOperational && !engine.getFlameoutState)
                            {
                                //double isp = engine.atmosphereCurve.Evaluate((float)(vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres)) * engine.g;
                                //totalThrust += isp * engine.maxFuelFlow;
                                totalThrust += engine.maxThrust;
                            }
                        }
                    }
                }
                else if (part.Modules.Contains("ModuleEnginesFX"))
                {
                    foreach (PartModule module in part.Modules)
                    {
                        if (module != null && module is ModuleEnginesFX && module.isActiveAndEnabled)
                        {
                            ModuleEnginesFX engine = (ModuleEnginesFX)module;
                            if (engine.isOperational && !engine.getFlameoutState)
                            {
                                //double isp = engine.atmosphereCurve.Evaluate((float)(vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres)) * engine.g;
                                //totalThrust += isp * engine.maxFuelFlow;
                                totalThrust += engine.maxThrust;
                            }
                        }
                    }
                }
            }
            return totalThrust;
        }

        /*
        public string toSI(double d)
        {
            if (d >= 0)
            {
                int i = 0;
                string[] units = { "m", "km", "Mm", "Gm", "Tm" };
                for (i = 0; i <= 4; i++)
                {
                    if (d > 1000) { d /= 1000; } else { break; }
                }
                string result = string.Format("{0:0.000}", d);
                int p = result.IndexOf(".") + 3;
                return (result.Substring(0, p) + " " + units[i]);
            }
            else
            {
                return "----";
            }
        }
        */

        public double burnlength(double dv)
        {
            return dv / tmr();
        }

        public double thrustAccel()
        {
            tmr();
            double throttle = pdata.vessel.ctrlState.mainThrottle;
            return (1.0 - throttle) * pdata.minthrustaccel + throttle * pdata.maxthrustaccel;
        }

        public static int HoursPerDay { get { return GameSettings.KERBIN_TIME ? 6 : 24; } }
        public static int DaysPerYear { get { return GameSettings.KERBIN_TIME ? 426 : 365; } }

        public static string TimeToDHMS(double seconds, int decimalPlaces = 0)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds))
            {
                return "Inf";
            }

            string ret = "";
            bool showSecondsDecimals = decimalPlaces > 0;

            try
            {
                string[] postfixes = { "y ", "d ", ":", ":", "" };
                long[] intervals = { DaysPerYear * HoursPerDay * 3600, HoursPerDay * 3600, 3600, 60, 1 };

                if (seconds < 0)
                {
                    ret += "-";
                    seconds *= -1;
                }

                for (int i = 0; i < postfixes.Length; i++)
                {
                    long n = (long)(seconds / intervals[i]);
                    bool first = ret.Length < 2;
                    if (!first || n != 0 || i >= 2 || (i == postfixes.Length - 1 && ret == ""))
                    {
                        if (showSecondsDecimals && seconds < 60 && i == postfixes.Length -1)
                        {
                            ret += seconds.ToString("0." + new string('0', decimalPlaces));
                        }
                        else if (first)
                        {
                            ret += n.ToString();
                        }
                        else
                        {
                            ret += n.ToString("00");
                        }

                        ret += postfixes[i];
                    }
                    seconds -= n * intervals[i];
                }

            }
            catch (Exception)
            {
                return "NaN";
            }
            return ret;
        }

        //Puts numbers into SI format, e.g. 1234 -> "1.234 k", 0.0045678 -> "4.568 m"
        //maxPrecision is the exponent of the smallest place value that will be shown; for example
        //if maxPrecision = -1 and digitsAfterDecimal = 3 then 12.345 will be formatted as "12.3"
        //while 56789 will be formated as "56.789 k"
        public static string ToSI(double d, int maxPrecision = -99, int sigFigs = 4)
        {
            if (d == 0 || double.IsInfinity(d) || double.IsNaN(d))
            {
                return d.ToString() + " ";
            }

            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(d))); //exponent of d if it were expressed in scientific notation

            string[] units = new string[] {
                "y",
                "z",
                "a",
                "f",
                "p",
                "n",
                "μ",
                "m",
                "",
                "k",
                "M",
                "G",
                "T",
                "P",
                "E",
                "Z",
                "Y"
            };
            const int unitIndexOffset = 8; //index of "" in the units array
            int unitIndex = (int)Math.Floor(exponent / 3.0) + unitIndexOffset;
            if (unitIndex < 0)
            {
                unitIndex = 0;
            }
            if (unitIndex >= units.Length)
            {
                unitIndex = units.Length - 1;
            }
            string unit = units[unitIndex];

            int actualExponent = (unitIndex - unitIndexOffset) * 3; //exponent of the unit we will us, e.g. 3 for k.
            d /= Math.Pow(10, actualExponent);

            int digitsAfterDecimal = sigFigs - (int)(Math.Ceiling(Math.Log10(Math.Abs(d))));

            if (digitsAfterDecimal > actualExponent - maxPrecision)
            {
                digitsAfterDecimal = actualExponent - maxPrecision;
            }
            if (digitsAfterDecimal < 0)
            {
                digitsAfterDecimal = 0;
            }

            string ret = d.ToString("F" + digitsAfterDecimal) + " " + unit;

            return ret;

        }
    }
}

