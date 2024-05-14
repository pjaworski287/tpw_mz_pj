﻿using Data;
using System.Diagnostics;

// BallManagement - Warstwa logiki - Model

namespace Logic
{
    public interface IBallManagement
    {
        void MoveBalls();
        void SetBalls(int count);
        void SetBalls(List<Data.Data> balls);
        IEnumerable<Data.Data> GetBalls();
        Task HandleCollisionsAsync();
        Task StartGameAsync(int ballCount, Action<IEnumerable<Data.Data>> callback);
        Task UpdateGameAsync();

        event EventHandler<IEnumerable<Data.Data>> GameUpdated;
    }


    public class BallManagement : IBallManagement
    {

        private readonly Random random = new Random();
        private readonly List<Data.Data> balls = new List<Data.Data>();
        public event EventHandler<IEnumerable<Data.Data>> GameUpdated;
        private readonly object lockObject = new object();
        private readonly Rect gameArea;

        public BallManagement(int gameWidth, int gameHeight)
        {
            gameArea = new Rect(gameWidth, gameHeight);
        }

        public async Task StartGameAsync(int ballCount, Action<IEnumerable<Data.Data>> callback)
        {
            SetBalls(ballCount);
            callback?.Invoke(GetBalls());
        }

        private void NotifyGameUpdated()
        {
            GameUpdated?.Invoke(this, GetBalls());

            Debug.WriteLine("Notify");
        }

        public async Task UpdateGameAsync()
        {
            await HandleCollisionsAsync();
            MoveBalls();
            Debug.WriteLine("UpdateGame");
            NotifyGameUpdated(); // Wywołanie zdarzenia informującego o aktualizacji stanu gry
        }

        private void CheckCollisionWithWalls(Data.Data ball)
        {
            if (ball.X - ball.Diameter / 2 <= 0 || ball.X + ball.Diameter / 2 >= gameArea.width)
                ball.Vel_X = -ball.Vel_X;
           
            if (ball.Y - ball.Diameter / 2 <= 0 || ball.Y + ball.Diameter / 2 >= gameArea.height)
                ball.Vel_Y = -ball.Vel_Y;
        }

        public async Task HandleCollisionsAsync()
        {
            await Task.Run(() =>
            {
                lock (lockObject)
                {
                    for (int i = 0; i < balls.Count; i++)
                    {
                        for (int j = i + 1; j < balls.Count; j++)
                        {
                            if (CheckCollision(balls[i], balls[j]))
                            {
                                HandleCollision(balls[i], balls[j]);
                            }
                        }
                    }

                    foreach (var ball in balls)
                    {
                        CheckCollisionWithWalls(ball);
                    }
                }

                
            });
        }

        private bool CheckCollision(Data.Data ball1, Data.Data ball2)
        {
            double dx = ball1.X - ball2.X;
            double dy = ball1.Y - ball2.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double minDistance = ball1.Diameter / 2 + ball2.Diameter / 2;
            return distance < minDistance;
        }

        private void HandleCollision(Data.Data ball1, Data.Data ball2)
        {
            float tempVel_X = ball1.Vel_X;
            float tempVel_Y = ball1.Vel_Y;
            ball1.Vel_X = ball2.Vel_X;
            ball1.Vel_Y = ball2.Vel_Y;
            ball2.Vel_X = tempVel_X;
            ball2.Vel_Y = tempVel_Y;


            /*            ball1.Vel_X = -ball1.Vel_X;
                        ball1.Vel_Y = -ball1.Vel_Y;
                        ball2.Vel_X = -ball2.Vel_X;
                        ball2.Vel_Y = -ball2.Vel_Y;*/
        }
        
        public void MoveBalls()
        {
            lock (lockObject)
            {
                foreach (var ball in GetBalls())
                {
                    ball.X += ball.Vel_X;
                    ball.Y += ball.Vel_Y;
                }
            }
        }

        public void SetBalls(int count)
        {
            lock (lockObject)
            {
                balls.Clear();
                for (int i = 0; i < count; i++)
                {
                    Data.Data newBall;
                    bool collisionDetected;
                    do{
                        newBall = new Data.Data
                        {
                            X = random.Next(25, (int)gameArea.width - 25),
                            Y = random.Next(25, (int)gameArea.height - 25),
                            Weight = (float)(random.NextDouble() * 2 + 1),
                        };
                        float speedMultiplier = 2 / newBall.Weight;
                        newBall.Vel_X = GenRandVel() * speedMultiplier;
                        newBall.Vel_Y = GenRandVel() * speedMultiplier;
                        newBall.Diameter = newBall.Weight * 20;

                        collisionDetected = balls.Any(existingBall => CheckCollision(newBall, existingBall));
                    } while (collisionDetected);

                    balls.Add(newBall);
                }
            }
        }

        public void SetBalls(List<Data.Data> balls)
        {
            lock (lockObject)
            {
                this.balls.Clear();
                this.balls.AddRange(balls);
            }
        }

        public IEnumerable<Data.Data> GetBalls()
        {
            lock (lockObject)
            {
                return new List<Data.Data>(balls);
            }
        }

        private float GenRandVel()
        {
            return (float)(random.NextDouble() * 4 - 2);
        }
    }
}
