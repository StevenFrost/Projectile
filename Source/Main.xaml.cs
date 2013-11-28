using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Source {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window {
                // Public timer variables
        public DispatcherTimer dispatcherTimer;
        public float dispatcherGravitationalConst;
        public float dispatcherVelocity;
        public float dispatcherTheta;
        public float dispatcherInitialAltitude;
        public float dispatcherInitialDisp;
        public float dispatcherStep;
        public float dispatcherStepLinearInterval;
        public Stopwatch AnimatedFlightTime;

        public Main() {
            InitializeComponent();
        }

        private void DisplayButton_Click(object sender, RoutedEventArgs e) {
            float gravitationalConst = float.Parse(GravitationalBox.Text.ToString());
            float theta = float.Parse(AngleBox.Text.ToString());
            float velocity = float.Parse(VelocityBox.Text.ToString());
            float initialAltitude = float.Parse(InitialAltBox.Text.ToString());
            float initialDisp = float.Parse(InitialDispBox.Text.ToString());
            float coefRestitution = float.Parse(RestBox.Text.ToString());
            int numBounces = int.Parse(BouncesBox.Text.ToString());
            float maximumDistance = 0.0f;
            float maximumAltitude = 0.0f;
            float stepIncrementor = float.Parse(IncrementBox.Text.ToString());

            clearPath();

            // Calculate the maximum X and Y vailes for the curve
            maximumDistance = findMaximumDistance(gravitationalConst, theta, velocity);
            maximumAltitude = findHeightAtDistance(gravitationalConst, theta, velocity, maximumDistance / 2, initialAltitude);

            for (float step = 0.0f; step < maximumDistance; step += stepIncrementor)
                drawLineSegment(gravitationalConst, velocity, theta, initialAltitude, initialDisp, step, stepIncrementor);

            // Display projectile details
            TimeContent.Content = calculateFlightTime(theta, velocity, maximumDistance);
            HeightContent.Content = maximumAltitude;
            DistanceContent.Content = maximumDistance;
            FinalVelocityContent.Content = velocity;

            // Handle further bounces
            if (UseBouncesCheck.IsChecked == true) {
                for (int bounce = 0; bounce < numBounces; bounce++) {
                    float bounceVelocity = calculatePostBounceVelocity(coefRestitution, float.Parse(FinalVelocityContent.Content.ToString()));
                    float bounceDistance = findMaximumDistance(gravitationalConst, theta, bounceVelocity);
                    float bounceAltitude = findHeightAtDistance(gravitationalConst, theta, bounceVelocity, bounceDistance / 2, initialAltitude);

                    for (float step = 0.0f; step < bounceDistance; step += stepIncrementor)
                        drawLineSegment(gravitationalConst, bounceVelocity, theta, initialAltitude, float.Parse(DistanceContent.Content.ToString()), step, stepIncrementor);

                    TimeContent.Content = float.Parse(TimeContent.Content.ToString()) + calculateFlightTime(theta, bounceVelocity, bounceDistance);
                    DistanceContent.Content = float.Parse(DistanceContent.Content.ToString()) + bounceDistance;
                    FinalVelocityContent.Content = bounceVelocity;
                }
            }
        }

        private void AnimationButton_Click(object sender, RoutedEventArgs e) {
            if (AnimationButton.Content.ToString() == "Start Animation") {
                float gravitationalConst = float.Parse(GravitationalBox.Text.ToString());
                float theta = float.Parse(AngleBox.Text.ToString());
                float velocity = float.Parse(VelocityBox.Text.ToString());
                float initialAltitude = float.Parse(InitialAltBox.Text.ToString());
                float initialDisp = float.Parse(InitialDispBox.Text.ToString());
                float maximumDistance = 0.0f;
                float maximumAltitude = 0.0f;
                int totalFlightTime = 0;
                int numTimerLoops = 0;
                int timerResolution = int.Parse(IntervalBox.Text.ToString());

                AnimationButton.Content = "Stop Animation";
                ClearWindowButton.IsEnabled = false;
                clearPath();

                // Calculate the maximum X and Y vailes for the curve
                maximumDistance = findMaximumDistance(gravitationalConst, theta, velocity);
                maximumAltitude = findHeightAtDistance(gravitationalConst, theta, velocity, maximumDistance / 2, initialAltitude);

                // Find the total flight time and round it to the nearest 10ms
                totalFlightTime = (((int)Math.Round((calculateFlightTime(theta, velocity, maximumDistance)) * 1000, 0)) / 10) * 10;

                // Create the dispatcher timer object
                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, timerResolution);

                // Set the timer global variables
                dispatcherGravitationalConst = gravitationalConst;
                dispatcherVelocity = velocity;
                dispatcherTheta = theta;
                dispatcherInitialAltitude = initialAltitude;
                dispatcherInitialDisp = initialDisp;
                dispatcherStep = 0.0f;
                dispatcherStepLinearInterval = ((velocity * (float)Math.Cos(ConvertToRadians(theta))) * ((float)(timerResolution * 1E-3)));

                // Configurure the dispatcher timer
                numTimerLoops = totalFlightTime / timerResolution;
                dispatcherTimer.Tag = numTimerLoops;

                // Create a new instance of the global stopwatch
                AnimatedFlightTime = new Stopwatch();
                AnimatedFlightTime.Reset();

                // Start the stopwatch and timer
                AnimatedFlightTime.Start();
                dispatcherTimer.Start();
            } else {
                stopAnimation();
            }
        }

        private void ClearWindowButton_Click(object sender, RoutedEventArgs e) {
            clearPath();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            if ((int)dispatcherTimer.Tag > 0) {
                // Draw the line segment for the current time interval
                drawLineSegment(dispatcherGravitationalConst, dispatcherVelocity, dispatcherTheta, dispatcherInitialAltitude, dispatcherInitialDisp, dispatcherStep, dispatcherStepLinearInterval);

                // Update the time label
                TimeContent.Content = AnimatedFlightTime.Elapsed.TotalSeconds.ToString();

                // Increase the x coordinate value and decrease the timer tag
                dispatcherStep += dispatcherStepLinearInterval;
                dispatcherTimer.Tag = (int)dispatcherTimer.Tag - 1;
            } else {
                stopAnimation();
            }
        }

        private float findMaximumDistance(float gravitationalConst, float theta, float velocity) {
            return (float)((Math.Pow(velocity, 2) * Math.Sin(2 * ConvertToRadians(theta))) / (gravitationalConst));
        }

        private float findHeightAtDistance(float gravitationalConst, float theta, float velocity, float distance, float initialAltitude) {
            return (float)(initialAltitude + (distance * Math.Tan(ConvertToRadians(theta))) - ((gravitationalConst * Math.Pow(distance, 2)) / (2 * Math.Pow((velocity * Math.Cos(ConvertToRadians(theta))), 2))));
        }

        private float calculatePointMultiplier(float physicalSize, float clientSize) {
            return clientSize / physicalSize;
        }

        private float calculateFlightTime(float theta, float velocity, float distance) {
            return (float)((distance) / (velocity * Math.Cos(ConvertToRadians(theta))));
        }

        private float calculatePostBounceVelocity(float restitution, float velocity) {
            return restitution * velocity;
        }

        private float ConvertToRadians(float angle) {
            return (float)((Math.PI / 180) * angle);
        }

        private void drawLineSegment(float gravitationalConst, float velocity, float theta, float initialAltitude, float initialDisp, float step, float stepIncrementor) {
            Line lineSegment = new Line();
            lineSegment.Stroke = System.Windows.Media.Brushes.Black;
            lineSegment.X1 = initialDisp + step;
            lineSegment.X2 = initialDisp + (step + stepIncrementor);
            lineSegment.Y1 = DrawingCanvas.ActualHeight - (findHeightAtDistance(gravitationalConst, theta, velocity, step, initialAltitude));
            lineSegment.Y2 = DrawingCanvas.ActualHeight - (findHeightAtDistance(gravitationalConst, theta, velocity, step + stepIncrementor, initialAltitude));
            lineSegment.HorizontalAlignment = HorizontalAlignment.Left;
            lineSegment.VerticalAlignment = VerticalAlignment.Center;
            lineSegment.StrokeThickness = 1;

            DrawingCanvas.Children.Add(lineSegment);
        }

        private void stopAnimation() {
            // Stop the stopwatch and timer
            AnimatedFlightTime.Stop();
            dispatcherTimer.Stop();

            // Reset the timer object and button text
            dispatcherTimer = null;
            ClearWindowButton.IsEnabled = true;
            AnimationButton.Content = "Start Animation";
        }

        private void clearPath() {
            // Clear the canvas
            DrawingCanvas.Children.Clear();

            // Clean up the timer
            dispatcherTimer = null;
            AnimatedFlightTime = null;

            // Reset global variables
            dispatcherGravitationalConst = 0.0f;
            dispatcherVelocity = 0.0f;
            dispatcherTheta = 0.0f;
            dispatcherInitialAltitude = 0.0f;
            dispatcherInitialDisp = 0.0f;
            dispatcherStep = 0.0f;
            dispatcherStepLinearInterval = 0.0f;

            // Reset the projectile statistics
            TimeContent.Content = "0.000";
            HeightContent.Content = "0.000";
            DistanceContent.Content = "0.000";
            FinalVelocityContent.Content = "0.000";
        }
    }
}
