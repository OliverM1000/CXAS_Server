using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.CXAS
{
    public abstract class CRegionMotion
    {
        #region Constants        
        //----------------------------------------------------
        public const double H_PLANCK    = 6.62607004e-34;
        public const double C_LIGHT     = 299792458;
        public const double ME          = 9.10938356e-31;
        public const double QE          = 1.60217662e-19;
        //----------------------------------------------------
        #endregion


        #region Variables
        //----------------------------------------------------
        public double[][] __trajectory;
        //----------------------------------------------------
        #endregion


        #region Properties
        //----------------------------------------------------
        public string Name { get; set; }
        public string MotionType { get; set; }
        public double[][] Trajectory { get { return __trajectory; } }
        
        //----------------------------------------------------
        #endregion


        //====================================================
        public CRegionMotion()
        {
            __trajectory = new double[2][];
            __trajectory[0] = new double[2];
            __trajectory[1] = new double[2];
        }
        //====================================================


        #region Abstract Methods
        //----------------------------------------------------
        public abstract Int32 MakeTrajectory(ref string _error);
        public abstract string ToString();
        //----------------------------------------------------
        #endregion
    }



    public class CRegionSMotion : CRegionMotion
    {
        #region Constants
        //----------------------------------------------------
        private Int32 MIN_POINTS    = 100;
        private Int32 MAX_POINTS    = 10000;
        private double MIN_EDOT     = 1.0;
        private double MAX_EDOT     = 200.0;
        private double MIN_EDOTDOT  = 1.0;
        private double MAX_EDOTDOT  = 500.0;
        private double MIN_E        = 2000.0;
        private double MAX_E        = 34000.0;
        //----------------------------------------------------        
        private double MIN_TRIGGER_TIME = 0.005;
        private double MAX_TRIGGER_TIME = 4.0;
        //----------------------------------------------------
        #endregion


        #region Variables
        //----------------------------------------------------
        private double __a3;
        private double __a4;
        private double __b0;
        //----------------------------------------------------
        private double __t_total;
        private double __t_ab;
        //----------------------------------------------------
        #endregion


        #region Properties
        //----------------------------------------------------
        public string Element { get; set; }
        public string Edge { get; set; }
        //----------------------------------------------------
        public Int32 Points { get; set; }
        public double EDot { get; set; }        // in units of eV per second
        public double EDotDot { get; set; }     // in units of eV per second per second
        public double EEdge { get; set; }       // in units of eV
        public double E1 { get; set; }          // in units of eV
        public double E2 { get; set; }          // in units of eV
        //----------------------------------------------------
        #endregion


        //====================================================
        public CRegionSMotion() : base()
        {
            base.MotionType = "S";

            // start with some defaults
            this.Name = "Cu-K_DEMO";
            this.Element = "Cu";
            this.Edge = "K";
            this.Points = 2000;
            this.EEdge = 8979.0;
            this.E1 = this.EEdge - 150.0;
            this.E2 = this.EEdge + 350.0;
            this.EDot = 20.0;
            this.EDotDot = 50.0;
        }
        //====================================================


        #region Private Methods
        //----------------------------------------------------
        private Int32 ValidateConfig(ref string _error)
        {
            if (this.EEdge < MIN_E || this.EEdge > MAX_E)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "EEdge " + this.EEdge.ToString("F2");
                return -1;
            }

            if (this.E1 < MIN_E || this.E1 > MAX_E)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "E1" + this.E1.ToString("F2");
                return -1;
            }

            if (this.E2 < MIN_E || this.E2 > MAX_E)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "E2 " + this.E2.ToString("F2");
                return -1;
            }

            if (this.E1 > this.E2)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INCONSISTENCY " + "E1 " + this.E1.ToString("F2") + " E2 " + this.E2.ToString("F2");
                return -1;
            }

            if (this.EEdge < this.E1 || this.EEdge > this.E2)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INCONSISTENCY " + "EEdge " + this.EEdge.ToString("F2") + " E2 " + this.E2.ToString("F2");
                return -1;
            }

            if (this.EDot < MIN_EDOT || this.EDot > MAX_EDOT)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "EDot " + this.EDot.ToString("F2");
                return -1;
            }

            if (this.EDotDot < MIN_EDOTDOT || this.EDotDot > MAX_EDOTDOT)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "EDotDot " + this.EDotDot.ToString("F2"); ;
                return -1;
            }

            return 1;
        }
        private Int32 ValidateTrajectory(ref string _error)
        {
            double minTriggerTime, maxTriggerTime;
            double tmp;

            
            // find min and max trigger time
            minTriggerTime = __trajectory[0][__trajectory[0].Length - 1];
            maxTriggerTime = 0;
            for (Int32 i = 0; i < __trajectory[0].Length - 1; i++)
            {
                tmp = __trajectory[0][i + 1] - __trajectory[0][i];

                if (tmp < minTriggerTime)
                    minTriggerTime = tmp;

                if (tmp > maxTriggerTime)
                    maxTriggerTime = tmp;
            }


            if (minTriggerTime < MIN_TRIGGER_TIME)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "minTriggerTime " + minTriggerTime.ToString("F3");
                return -1;
            }

            if (maxTriggerTime > MAX_TRIGGER_TIME)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "maxTriggerTime " + maxTriggerTime.ToString("F3");
                return -1;
            }
            

            // check if trajectory is stricly monotonic rising
            for (int i = 0; i < __trajectory[1].Length - 1; i++)
            {
                if (__trajectory[1][i] > __trajectory[1][i + 1])
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "INCONSISTENCY " + "__trajectory";
                    return -1;
                }
                
            }

            return 1;
        }
        private Int32 calcluate_user_constants(ref string _error)
        {
            __t_ab = (3.0 * this.EDot) / (2.0 * this.EDotDot);
            __t_total = __t_ab + (this.E2 - this.E1) / this.EDot;

            __a3 = (4.0 * this.EDotDot * this.EDotDot) / (9.0 * this.EDot);
            __a4 = (-4.0 * this.EDotDot * this.EDotDot * this.EDotDot) / (27.0 * this.EDot * this.EDot); ;
            __b0 = (-9.0 * this.EDot * this.EDot) / (4.0 * this.EDotDot) + this.E1;

            if (2 * __t_ab >= __t_total)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error = "INCONSISTENCY " + "__t_total";
                return -1;
            }
            
            return 0;
        }
        //----------------------------------------------------
        #endregion


        #region Override Methods
        //----------------------------------------------------
        public override Int32 MakeTrajectory(ref string _error)
        {
            Int32 rt;
            double time;
            double dt;
            

            if (this.Points < MIN_POINTS)
                this.Points = MIN_POINTS;
            if (this.Points > MAX_POINTS)
                this.Points = MAX_POINTS;


            __trajectory = new double[2][];
            __trajectory[0] = new double[this.Points];   // Time
            __trajectory[1] = new double[this.Points];   // Energy

            rt = ValidateConfig(ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
                

            rt = calcluate_user_constants(ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
             
            
            
            dt = __t_total / this.Points;

            time = 0;
            for (Int32 i = 0; i < this.Points; i++)
            {
                if (time < __t_ab)
                {
                    __trajectory[0][i] = time;
                    __trajectory[1][i] = this.E1 + __a3 * Math.Pow((time), 3) + __a4 * Math.Pow((time), 4);
                }
                else if (time >= __t_ab && time <= __t_total - __t_ab)
                {
                    __trajectory[0][i] = time;
                    __trajectory[1][i] = __b0 + this.EDot * (time + __t_ab);
                }
                else if (time > __t_total - __t_ab && time <= __t_total)
                {
                    __trajectory[0][i] = time;
                    __trajectory[1][i] = this.E2 + __a3 * Math.Pow(time - __t_total, 3) - __a4 * Math.Pow(time - __t_total, 4);
                }
                else if (time > __t_total)
                {
                    __trajectory[0][i] = time;
                    __trajectory[1][i] = this.E2;
                }

                time += dt;
            }

            rt = ValidateTrajectory(ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
                

            return 1;
        }
        public override string ToString()
        {
            string s;

            s = base.MotionType;
            s += " " + this.Name;
            s += " " + this.Element;
            s += " " + this.Edge;
            s += " " + this.Trajectory[0].Length.ToString();
            s += " " + this.EEdge.ToString("F4");
            s += " " + this.E1.ToString("F4");
            s += " " + this.E2.ToString("F4");
            s += " " + this.EDot.ToString("F2");
            s += " " + this.EDotDot.ToString("F2");

            for (Int32 i = 0; i < this.Trajectory[0].Length; i++)
            {                
                s += "\r" + this.Trajectory[0][i].ToString("F4");
                s += " " + this.Trajectory[1][i].ToString("F4");
            }

            return s;
        }    
        //----------------------------------------------------
        #endregion
    }



    public class CRegionExafsMotion : CRegionMotion
    {

        #region Constants
        //----------------------------------------------------
        private Int32 HIGH_RES_FACTOR = 200;
        //----------------------------------------------------
        private Int32 MIN_POINTS    = 100;
        private Int32 MAX_POINTS    = 10000;
        private double MIN_K0       = 1.0;
        private double MAX_K0       = 20.0;
        private double MIN_K0DOT    = 0.01;
        private double MAX_K0DOT    = 10.0;
        private double MIN_E        = 2000.0;
        private double MAX_E        = 34000.0;
        private Int32 MIN_SCALING   = 2;
        private Int32 MAX_SCALING   = 3;
        private double MIN_TTA     = 1.0;
        private double MAX_TTA     = 60.0;
        private double MIN_TTD     = 1.0;
        private double MAX_TTD     = 60.0;
        //----------------------------------------------------
        private double MIN_TRIGGER_TIME = 0.005;
        private double MAX_TRIGGER_TIME = 4.0;
        //----------------------------------------------------
        #endregion


        #region Variables
        //----------------------------------------------------
        private double __c_tilde;       // user constant
        private double __c_0;           // user constant
        private double __beta;          // user constant
        private double __tta;           // time to accelerate
        private double __ttd;           // time to deaccelerate
        private double __tem;           // time in EXAFS speed mode
        //----------------------------------------------------
        #endregion
        

        #region Properties
        //----------------------------------------------------
        public string Element { get; set; }
        public string Edge { get; set; }
        //----------------------------------------------------
        public Int32 TargetPoints { get; set; }
        public Int32 Scaling { get; set; }
        public double k0 { get; set; }      // in units of inverse Angstrom
        public double k0Dot { get; set; }   // in units of inverse Angstrom per second
        public double EEdge { get; set; }   // in units of eV
        public double E1 { get; set; }      // in units of eV
        public double E2 { get; set; }      // in units of eV
        public double TTA { get; set; }    // factor: time to accelerate
        public double TTD { get; set; }    // factor: time to deaccelerate
        public double TEM { get { return __tem; } }    // factor: time in EXAFS mode
        //----------------------------------------------------
        #endregion


        //====================================================
        public CRegionExafsMotion() : base()
        {
            base.MotionType = "EXAFS";


            // start with some defaults
            this.Name = "Cu-K_DEMO";
            this.Element = "Cu";
            this.Edge = "K";

            this.TargetPoints = 5000;
            this.Scaling = 2;
            this.k0 = 4.0;
            this.k0Dot = 1.0;
            this.EEdge = 8979.0;
            this.E1 = this.EEdge - 200.0;
            this.E2 = this.EEdge + 1100.0;

            this.TTA = 10.0;
            this.TTD = 1.0;
        }
        //====================================================


        #region Private Methods
        //----------------------------------------------------
        
        private Int32 ValidateConfig(ref string _error)
        {
            

            if (this.TargetPoints < MIN_POINTS)
                this.TargetPoints = MIN_POINTS;
            if (this.TargetPoints > MAX_POINTS)
                this.TargetPoints = MAX_POINTS;


            if (this.EEdge < MIN_E || this.EEdge > MAX_E)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "EEdge " + this.EEdge.ToString("F2");
                return -1;
            }
            
            if (this.E1 < MIN_E || this.E1 > MAX_E)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "E1" + this.E1.ToString("F2");
                return -1;
            }

            if (this.E2 < MIN_E || this.E2 > MAX_E)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "E2 " + this.E2.ToString("F2");
                return -1;
            }

            if (this.E1 > this.E2)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INCONSISTENCY " + "E1 " + this.E1.ToString("F2") + " E2 " + this.E2.ToString("F2");
                return -1;
            }
            
            if (this.EEdge < this.E1 || this.EEdge > this.E2)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INCONSISTENCY " + "EEdge " + this.EEdge.ToString("F2") + " E2 " + this.E2.ToString("F2");
                return -1;
            }

            if (this.k0 < MIN_K0 || this.k0 > MAX_K0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "K0 " + this.k0.ToString("F2");
                return -1;
            }

            if (this.k0Dot < MIN_K0DOT || this.k0Dot > MAX_K0DOT)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "KDOT " + this.k0Dot.ToString("F2");
                return -1;
            }

            if (this.Scaling < MIN_SCALING || this.Scaling > MAX_SCALING)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "SCALING " + this.Scaling.ToString("F0");
                return -1;
            }

            if (this.TTA < MIN_TTA || this.TTA > MAX_TTA)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "TTA " + this.TTA.ToString("F2");
                return -1;
            }

            if (this.TTD < MIN_TTD || this.TTD > MAX_TTD)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "TTD " + this.TTD.ToString("F2");
                return -1;
            }

            return 1;
        }
        private Int32 ValidateTrajectory(ref string _error)
        {
            

            double minTriggerTime, maxTriggerTime;
            double tmp;

            // find min and max trigger time
            minTriggerTime = __trajectory[0][__trajectory[0].Length - 1];
            maxTriggerTime = 0;
            for (Int32 i = 0; i < __trajectory[0].Length - 1; i++)
            {
                tmp = __trajectory[0][i + 1] - __trajectory[0][i];

                if (tmp < minTriggerTime)
                    minTriggerTime = tmp;

                if (tmp > maxTriggerTime)
                    maxTriggerTime = tmp;
            }

            if (minTriggerTime < MIN_TRIGGER_TIME)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "minTriggerTime " + minTriggerTime.ToString("F4");
                return -1;
            }

            if (maxTriggerTime > MAX_TRIGGER_TIME)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "minTriggerTime " + maxTriggerTime.ToString("F4");
                return -1;
            }


            // check if trajectory is stricly monotonic rising
            for (int i = 0; i < __trajectory[1].Length - 1; i++)
            {
                if (__trajectory[1][i] > __trajectory[1][i + 1])
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "INCONSISTENCY " + "__trajectory";
                    return -1;
                }
                
            }
            return 1;
        }
        //----------------------------------------------------
        private void SolveTransition12(out double[] _pv)
        {
            calculate_user_times();

            double[] dv;
            GetDesignVector(0, E1, out dv);

            double[,] dm;
            GetDesignMatrix(__tta, out dm);

            // singular value decomposition			
            double[] w;
            double[,] v;
            Numerics.SVD.Decompose(ref dm, 3, 3, out w, out v);
            Numerics.SVD.Reorder(ref dm, ref w, ref v, 3, 3);

            // solve linear equations			
            Numerics.SVD.Solve(dm, w, v, dv, 3, 3, out _pv);
        }
        private void SolveTransition23(out double[] _pv)
        {
            calculate_user_times();

            double[] dv;
            GetDesignVector(__tem, E2, out dv);

            double[,] dm;
            GetDesignMatrix(-1.0 * __ttd, out dm);

            // singular value decomposition			
            double[] w;
            double[,] v;
            Numerics.SVD.Decompose(ref dm, 3, 3, out w, out v);
            Numerics.SVD.Reorder(ref dm, ref w, ref v, 3, 3);

            // solve linear equations			
            Numerics.SVD.Solve(dm, w, v, dv, 3, 3, out _pv);
        }
        private void GetDesignMatrix(double _t, out double[,] _dm)
        {
            _dm = new double[3, 3];

            double t_2, t_3, t_4;
            t_2 = _t * _t;
            t_3 = _t * t_2;
            t_4 = _t * t_3;

            _dm[0, 0] = t_3;
            _dm[1, 0] = 3 * t_2;
            _dm[2, 0] = 6 * _t;

            _dm[0, 1] = t_4;
            _dm[1, 1] = 4 * t_3;
            _dm[2, 1] = 12 * t_2;

            _dm[0, 2] = t_4 * _t;
            _dm[1, 2] = 5 * t_4;
            _dm[2, 2] = 20 * t_3;
        }
        private void GetDesignVector(double _t, double _E, out double[] _dv)
        {
            _dv = new double[3];

            _dv[0] = __beta * Math.Pow(_t + __c_0, 2.0 / ((double)this.Scaling + 1.0)) + (this.EEdge - _E) * QE;
            _dv[1] = 2.0 / ((double)this.Scaling + 1.0) * __beta * Math.Pow(_t + __c_0, (1.0 - (double)this.Scaling) / ((double)this.Scaling + 1.0));
            _dv[2] = 2.0 * (1.0 - (double)this.Scaling) / (((double)this.Scaling + 1.0) * ((double)this.Scaling + 1.0)) * __beta * Math.Pow(_t + __c_0, -2.0 * (double)this.Scaling / ((double)this.Scaling + 1.0));
        }        
        //----------------------------------------------------
        private void calcluate_user_constants()
        {
            double tmp_n;

            tmp_n = 1.0 / ((double)this.Scaling + 1.0);

            __c_tilde = Math.Pow(this.Scaling + 1, tmp_n) * Math.Pow(this.k0 * 1e10, this.Scaling * tmp_n) * Math.Pow(this.k0Dot * 1e10, tmp_n);
            __c_0 = tmp_n * this.k0 / this.k0Dot;
            __beta = H_PLANCK * H_PLANCK * __c_tilde * __c_tilde / (8 * Math.PI * Math.PI * ME);
        }
        private void calculate_user_times()
        {
            double tmp_k;   // k at E_2
            double tmp_t;   // time to move from k_0 to k_1

            tmp_k = Math.Sqrt(2.0 * QE * ME * (this.E2 - this.EEdge)) * 2.0 * Math.PI / H_PLANCK;
            tmp_t = Math.Pow(tmp_k / __c_tilde, (double)this.Scaling + 1) - __c_0;

            __tta = this.TTA;
            __tem = tmp_t;
            __ttd = this.TTD;
        }
        //----------------------------------------------------
        #endregion



        #region Override Methods
        //----------------------------------------------------
        public override int MakeTrajectory(ref string _error)
        {
            Int32 rt;
            Int32 i, j, k, l;
            double[] pv_12;
            double[] pv_23;

            Int32 points;

            double totalTime;
            double totalTriggerTime;

            double time;
            double dt;
            double dE;
            double tmp_dE;

            double[][] highResTrajectory;
            double[][] triggerTime;


            

            rt = ValidateConfig(ref _error);
            if (rt < 0)
            {
                __trajectory = new double[2][];
                __trajectory[0] = new double[MIN_POINTS];
                __trajectory[1] = new double[MIN_POINTS];
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return - 1;
            }

            calcluate_user_constants();
            calculate_user_times();
                        
            totalTime = __tta + __tem + __ttd;

            highResTrajectory = new double[2][];
            highResTrajectory[0] = new double[this.TargetPoints * HIGH_RES_FACTOR]; // Time
            highResTrajectory[1] = new double[this.TargetPoints * HIGH_RES_FACTOR]; // Energy	

            triggerTime = new double[2][];
            triggerTime[0] = new double[(this.TargetPoints * HIGH_RES_FACTOR) + 1];      // time
            triggerTime[1] = new double[(this.TargetPoints * HIGH_RES_FACTOR) + 1];      // time base

            
            SolveTransition12(out pv_12);            
            SolveTransition23(out pv_23);


            // 1) get a high resolution trajectory on an equidistant time grid
            #region
            dt = totalTime / (this.TargetPoints * HIGH_RES_FACTOR);     // equidistant time
            time = 0;
            for (i = 0; i < this.TargetPoints * HIGH_RES_FACTOR; i++)
            {
                if (time < __tta)
                {
                    highResTrajectory[0][i] = time;
                    highResTrajectory[1][i] = (pv_12[0] * Math.Pow(time, 3) + pv_12[1] * Math.Pow(time, 4) + pv_12[2] * Math.Pow(time, 5)) / QE + this.E1;
                }
                else if (time < __tta + __tem)
                {
                    highResTrajectory[0][i] = time;
                    highResTrajectory[1][i] = H_PLANCK * H_PLANCK * __c_tilde * __c_tilde / (8 * Math.PI * Math.PI * ME) * Math.Pow((time + __c_0 - __tta), 2 / ((double)this.Scaling + 1)) / QE + this.EEdge;
                }
                else if (time <= __tta + __tem + __ttd)
                {
                    highResTrajectory[0][i] = time;
                    highResTrajectory[1][i] = (pv_23[0] * Math.Pow(time - __ttd - __tta - __tem, 3) + pv_23[1] * Math.Pow(time - __ttd - __tta - __tem, 4) + pv_23[2] * Math.Pow(time - __ttd - __tta - __tem, 5)) / QE + this.E2;
                }
                else if (time > __tta + __tem + __ttd)
                {
                    highResTrajectory[0][i] = time;
                    highResTrajectory[1][i] = this.E2;
                }

                time += dt;
            }
            #endregion


            // 2) get a new time grid which keeps delta_E constant			
            #region
            j = 0;
            k = 0;            
            points = 0;
            totalTriggerTime = 0;
            dE = (this.E2 - this.E1) / this.TargetPoints;

            while (j < (this.TargetPoints * HIGH_RES_FACTOR) - 1)
            {
                tmp_dE = 0;
                l = 0;
                while (tmp_dE < dE)
                {
                    tmp_dE = highResTrajectory[1][j + l] - highResTrajectory[1][j];
                    l++;
                    if (j + l > (this.TargetPoints * HIGH_RES_FACTOR) - 1)
                        break;
                }
                triggerTime[0][k] = highResTrajectory[0][j];
                triggerTime[1][k] = highResTrajectory[0][j + l - 1] - highResTrajectory[0][j];

                totalTriggerTime += triggerTime[1][k];

                k++;
                j += l - 1;
            }
            points = k;
            #endregion


            __trajectory = new double[2][];
            __trajectory[0] = new double[points];
            __trajectory[1] = new double[points];


            // 3) calculate the trajectory on the new reduced time grid
            #region
            time = 0;
            for (i = 0; i < points; i++)
            {
                if (time < __tta)
                {
                    __trajectory[0][i] = time;
                    __trajectory[1][i] = (pv_12[0] * Math.Pow(time, 3) + pv_12[1] * Math.Pow(time, 4) + pv_12[2] * Math.Pow(time, 5)) / QE + this.E1;
                }
                else if (time < __tta + __tem)
                {
                    __trajectory[0][i] = time;
                    __trajectory[1][i] = H_PLANCK * H_PLANCK * __c_tilde * __c_tilde / (8 * Math.PI * Math.PI * ME) * Math.Pow((time + __c_0 - __tta), 2 / ((double)this.Scaling + 1)) / QE + this.EEdge;
                }
                else if (time <= __tta + __tem + __ttd)
                {
                    __trajectory[0][i] = time;
                    __trajectory[1][i] = (pv_23[0] * Math.Pow(time - __ttd - __tta - __tem, 3) + pv_23[1] * Math.Pow(time - __ttd - __tta - __tem, 4) + pv_23[2] * Math.Pow(time - __ttd - __tta - __tem, 5)) / QE + this.E2;
                }
                else if (time > __tta + __tem + __ttd)
                {
                    __trajectory[0][i] = time;
                    __trajectory[1][i] = this.E2;
                }

                time += triggerTime[1][i];
            }
            #endregion

            rt = ValidateTrajectory(ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
                

            return 1;
        }
        public override string ToString()
        {
            string s;

            s = base.MotionType;
            s += " " + this.Name;
            s += " " + this.Element;
            s += " " + this.Edge;
            s += " " + this.Trajectory[0].Length.ToString();
            s += " " + this.EEdge.ToString("F4");
            s += " " + this.E1.ToString("F4");
            s += " " + this.E2.ToString("F4");
            s += " " + this.k0.ToString("F2");
            s += " " + this.k0Dot.ToString("F2");
            s += " " + this.Scaling.ToString("F0");
            s += " " + this.TTA.ToString("F3");
            s += " " + this.TEM.ToString("F3");
            s += " " + this.TTD.ToString("F3");

            for (Int32 i = 0; i < this.Trajectory[0].Length; i++)
            {
                s += "\r" + this.Trajectory[0][i].ToString("F4");
                s += " " + this.Trajectory[1][i].ToString("F4");
            }

            return s;
        }
        //----------------------------------------------------
        #endregion

    }

}
