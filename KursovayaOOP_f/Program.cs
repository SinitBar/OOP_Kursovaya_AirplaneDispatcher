using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KursovayaOOP_f
{
    abstract class GenerateDelay
    {
        // задаем нормальное распределение как общее пока что
        
        const double a = 0; // мат ожидание
        const int max_delay = 120;
        public const int integral_step = 5; // он же есть минимальная учитываемая еденица времени
        const double sigma = max_delay/(integral_step*3); // дисперсия по правилу 3 сигм, при грамотной постановке все делится нацело 
        static double[] probability = new double[max_delay / (int)integral_step + 1];
        
        // функция плотности
        static double f(double x) // х обозначает отклонение от расписания в минутах
        { // с вероятностью 50% самолет будет вовремя, сильное опоздание практически нереально
            // для разброса влево и вправо от нуля на 120 максимальная сигма = 3 дисперсии = примерно 40
            // для разброса на 60 максимальная сигма = примерно 20
            // если берем только положительную часть распределения, f надо умножить на 2, чтобы площадь подграфика осталась = 1
            // возьмем только положительную часть
            double f_x = 2*Math.Exp(-(x - a) * (x - a) / (2 * sigma * sigma)) / (sigma * Math.Sqrt(2 * Math.PI));
            return f_x;
        }
        static void count_probabilities(ref double[] p_array)
        {
            for (int i = 0; i < p_array.Length; i++)
                p_array[i] = integral_step * f((i + 0.5) * integral_step); // для нуля это значения икса от 0 до 1
                // считаем интеграл по формуле средних прямоугольников с шагом = 5 мин
        }
        public static int generate_delay(ref Random random)
        {
            double rand_p = random.NextDouble(); // случайная вероятность
            //Console.WriteLine(rand_p);
            double sum = 0;
            for (int i = 0; i < probability.Length; i++)
            {
                if ((rand_p >= sum) && (rand_p < sum + probability[i]))
                {
                    //Console.WriteLine("p of {0}:  {1} -- {2}", i * integral_step, sum, sum + probability[i]);
                    return (i * integral_step);
                }
                sum += probability[i];
            }
            return 0;
        }
        public static int[] get_delays(int amount)
        { // amount - количество отклонений, которое нужно сгенерировать
            count_probabilities(ref probability);
            Random random = new Random();
            int[] delays = new int[amount];
            for (int i = 0; i < amount; i++)
                delays[i] = generate_delay(ref random);
            return delays;
        }
        static void Main(string[] args)
        {
            //count_probabilities(ref probability);
            //double sum = 0;
            //for (int i = 0; i < probability.Length; i++)
            //{
            //    sum += probability[i];
            //    Console.WriteLine("probability of delay on {0} min = {1}", i * integral_step, probability[i]);
            //}
            //Console.WriteLine(" sum = {0}", sum);

            int[] dels = get_delays(20);
            //Console.WriteLine();
            for (int i = 0; i < dels.Length; i++)
                Console.WriteLine(dels[i]);
            Console.Read();
        }
    }
}
