using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Source {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window {
        public Stopwatch animationFlightTime;
        public DispatcherTimer animationTimer;
        public double animationC; /*< Gravitational Constant */
        public double animationV; /*< Initial velocity */
        public double animationT; /*< Angle */
        public double animationA; /*< Initial vertical displacement */
        public double animationD; /*< Horizontal displacement */
        public double animationS; /*< Current step value */
        public double animationI; /*< Step interval */

        /// <summary>
        /// Default constructor
        /// </summary>
        public Main() {
            InitializeComponent();
        }

        /// <summary>
        /// Displays a static projectile path
        /// </summary>
        private void DisplayButton_Click(object sender, RoutedEventArgs e) {
            /* User paramaters */
            double r = double.Parse(RestBox.Text.ToString());
            double t = double.Parse(AngleBox.Text.ToString());
            double v = double.Parse(VelocityBox.Text.ToString());
            double i = double.Parse(IncrementBox.Text.ToString());
            double a = double.Parse(InitialAltBox.Text.ToString());
            double d = double.Parse(InitialDispBox.Text.ToString());
            double c = double.Parse(GravitationalBox.Text.ToString());

            /* Properties */
            double maxD = 0.0f;
            double maxA = 0.0f;
            clearPath();

            /* Calculate the maximum X and Y vailes for the curve */
            maxD = getMaximumDistance(c, t, v);
            maxA = getHeight(c, t, v, maxD / 2, a);

            for (double step = 0.0f; step < maxD; step += i) {
                drawLineSegment(c, v, t, a, d, step, i);
            }

            /* Display projectile details */
            TimeContent.Content = calculateFlightTime(t, v, maxD);
            HeightContent.Content = maxA;
            DistanceContent.Content = maxD;
            FinalVelocityContent.Content = v;

            /* Handle further bounces */
            if ((bool)UseBouncesCheck.IsChecked) {
                int bounces = int.Parse(BouncesBox.Text.ToString());
                for (int bounce = 0; bounce < bounces; bounce++) {
                    double bounceVelocity = calculatePostBounceVelocity(r,
                        float.Parse(FinalVelocityContent.Content.ToString()));
                    double bounceDistance = getMaximumDistance(c, t, bounceVelocity);
                    double bounceAltitude = getHeight(c, t, bounceVelocity, bounceDistance / 2, a);

                    for (double step = 0.0f; step < bounceDistance; step += i) {
                        drawLineSegment(c, bounceVelocity, t, a,
                            double.Parse(DistanceContent.Content.ToString()), step, i);
                    }

                    TimeContent.Content = double.Parse(TimeContent.Content.ToString()) +
                        calculateFlightTime(t, bounceVelocity, bounceDistance);
                    DistanceContent.Content = double.Parse(DistanceContent.Content.ToString()) +
                        bounceDistance;
                    FinalVelocityContent.Content = bounceVelocity;
                }
            }
        }

        /// <summary>
        /// Starts the projectile animation
        /// </summary>
        private void AnimationButton_Click(object sender, RoutedEventArgs e) {
            if (AnimationButton.Content.ToString() != "Start Animation") {
                stopAnimation();
                return;
            }

            /* User paramaters */
            double c = float.Parse(GravitationalBox.Text.ToString());
            double t = float.Parse(AngleBox.Text.ToString());
            double v = float.Parse(VelocityBox.Text.ToString());
            double a = float.Parse(InitialAltBox.Text.ToString());
            double d = float.Parse(InitialDispBox.Text.ToString());
            int pRes = int.Parse(IntervalBox.Text.ToString());

            /* Properties */
            double maxA = 0.0f;
            double maxD = 0.0f;
            int numTimerLoops = 0;
            int totalFlightTime = 0;

            /* Update the form controls */
            AnimationButton.Content = "Stop Animation";
            ClearWindowButton.IsEnabled = false;
            clearPath();

            /* Calculate the maximum X and Y vailes for the curve */
            maxD = getMaximumDistance(c, t, v);
            maxA = getHeight(c, t, v, maxD / 2.0, a);

            /* Find the total flight time and round it to the nearest 10ms */
            totalFlightTime = (((int)Math.Round((calculateFlightTime(t, v, maxD)) * 1000, 0)) / 10) * 10;

            /* Create the animation timer object */
            animationTimer = new DispatcherTimer();
            animationTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            animationTimer.Interval = new TimeSpan(0, 0, 0, 0, pRes);

            /* Set the animation variables */
            animationC = c;
            animationV = v;
            animationT = t;
            animationA = a;
            animationD = d;
            animationS = 0.0f;
            animationI = ((v * (double)Math.Cos(toRadians(t))) * ((double)(pRes * 1E-3)));

            /* Configurure the animation timer */
            numTimerLoops = totalFlightTime / pRes;
            animationTimer.Tag = numTimerLoops;

            /* Start the stopwatch */
            animationFlightTime = new Stopwatch();
            animationFlightTime.Reset();
            animationFlightTime.Start();
            animationTimer.Start();
        }

        /// <summary>
        /// Animation timer
        /// </summary>
        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            if ((int)animationTimer.Tag > 0) {
                drawLineSegment(animationC, animationV, animationT, animationA, animationD,
                    animationS, animationI);

                TimeContent.Content = animationFlightTime.Elapsed.TotalSeconds.ToString();
                animationS += animationI;
                animationTimer.Tag = (int)animationTimer.Tag - 1;
            } else {
                stopAnimation();
            }
        }

        /// <summary>
        /// Clears the canvas
        /// </summary>
        private void ClearWindowButton_Click(object sender, RoutedEventArgs e) {
            clearPath();
        }

        /// <summary>
        /// Calculates the maximum horizontal distance a projectile can travel before reaching the
        /// same vertical displacement it was released from
        /// </summary>
        /// <param name="c">The gravitational constant</param>
        /// <param name="t">The angle of release</param>
        /// <param name="v">The initial velocity of the projectile</param>
        /// <remarks>Valid for projectiles on a flat plane</remarks>
        /// <returns>The maximum distance traveled in metres</returns>
        private double getMaximumDistance(double c, double t, double v) {
            return (double)((Math.Pow(v, 2) * Math.Sin(2 * toRadians(t))) / (c));
        }

        /// <summary>
        /// Calculates the height of a projectile at a certain horizontal displacement
        /// </summary>
        /// <param name="c">The gravitational constant</param>
        /// <param name="t">The angle of release</param>
        /// <param name="v">The initial velocity of the projectile</param>
        /// <param name="d">the distance from the initial release point in metres</param>
        /// <param name="a">the initial vertical displacement of the projectile</param>
        /// <returns>The height of the projectile</returns>
        private double getHeight(double c, double t, double v, double d, double a) {
            return (double)(a + (d * Math.Tan(toRadians(t))) - ((c * Math.Pow(d, 2)) /
                (2 * Math.Pow(v * Math.Cos(toRadians(t)), 2))));
        }

        /// <summary>
        /// Calculates the point multiplier for the drawing window in order to draw to scale
        /// </summary>
        /// <param name="p">Physical size</param>
        /// <param name="c">Client size</param>
        /// <returns>The point multiplier</returns>
        private double calculatePointMultiplier(double p, double c) {
            return c / p;
        }

        /// <summary>
        /// Calculates the projectile's total flight time in seconds
        /// </summary>
        /// <param name="t">The angle of release</param>
        /// <param name="v">The initial velocity of the projectile</param>
        /// <param name="d">The total horizontal distance the projectile will travel</param>
        /// <remarks>Valid for projectiles on a flat plane</remarks>
        /// <returns>The flight time in seconds</returns>
        private double calculateFlightTime(double t, double v, double d) {
            return (double)((d) / (v * Math.Cos(toRadians(t))));
        }

        /// <summary>
        /// Calculates the velocity of the projectile after a bounce, based on its coefficient of
        /// restitution
        /// </summary>
        /// <param name="r">The coefficient of restitution of the projectile</param>
        /// <param name="v">The current velocity of the projectile</param>
        /// <returns>The velocity of the projectile after a bounce</returns>
        private double calculatePostBounceVelocity(double r, double v) {
            return r * v;
        }
        
        /// <summary>
        /// Converts an angle in degrees to radians
        /// </summary>
        /// <param name="angle">The angle in degrees to convert</param>
        /// <returns>The angle in radians</returns>
        private double toRadians(double angle) {
            return (double)((Math.PI / 180) * angle);
        }

        /// <summary>
        /// Draws one segment of a projectile path
        /// </summary>
        /// <param name="c">The gravitational constant</param>
        /// <param name="v">The initial velocity of the projectile</param>
        /// <param name="t">The angle of release</param>
        /// <param name="a">The initial vertical displacement of the projectile</param>
        /// <param name="d">The initial horizontal displacement of the projectile</param>
        /// <param name="s">The current step value</param>
        /// <param name="i">The step to increment by</param>
        private void drawLineSegment(double c, double v, double t, double a, double d, double s, double i) {
            Line line = new Line();

            /* Construct the line */
            line.StrokeThickness = 1;
            line.Stroke = System.Windows.Media.Brushes.Black;
            line.X1 = d + s;
            line.X2 = d + (s + i);
            line.Y1 = DrawingCanvas.ActualHeight - (getHeight(c, t, v, s, a));
            line.Y2 = DrawingCanvas.ActualHeight - (getHeight(c, t, v, s + i, a));
            line.HorizontalAlignment = HorizontalAlignment.Left;
            line.VerticalAlignment = VerticalAlignment.Center;

            /* Add the line to the drawing canvas */
            DrawingCanvas.Children.Add(line);
        }

        /// <summary>
        /// Stops the animation and re-enables controls
        /// </summary>
        private void stopAnimation() {
            animationFlightTime.Stop();
            animationTimer.Stop();

            animationTimer = null;
            ClearWindowButton.IsEnabled = true;
            AnimationButton.Content = "Start Animation";
        }

        /// <summary>
        /// Clears the canvas of any projectile paths
        /// </summary>
        private void clearPath() {
            /* Clear the canvas */
            DrawingCanvas.Children.Clear();

            /* Clean up the timer */
            animationTimer = null;
            animationFlightTime = null;

            /* Reset dispatcher variables */
            animationC = 0.0f;
            animationV = 0.0f;
            animationT = 0.0f;
            animationA = 0.0f;
            animationD = 0.0f;
            animationS = 0.0f;
            animationI = 0.0f;

            /* Reset the projectile statistics */
            TimeContent.Content = "0.000";
            HeightContent.Content = "0.000";
            DistanceContent.Content = "0.000";
            FinalVelocityContent.Content = "0.000";
        }
    }
}