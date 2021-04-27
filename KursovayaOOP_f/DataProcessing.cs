using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KursovayaOOP_f
{
    public struct durations
    {
        public string type;
        public int arrivalDuration;
        public int departureDuration;
        public durations(string airplaneType, int durationArrival, int durationDeparture)
        {
            type = airplaneType;
            arrivalDuration = durationArrival;
            departureDuration = durationDeparture;
        }
    }

    public class Runway
    {
        public List<Airplane> airplanesUse; // список самолетов, использующих в принципе данную полосу
        public Runway()
        {
            airplanesUse = new List<Airplane>();

        }
        bool is_free(DateTime atTime) // индикатор, занята или свободна полоса в момент atTime
        {
            foreach (Airplane airplane in airplanesUse)
            {
                // если в текущее время на этой полосе самолетный интервал, то занята
                DateTime min = new DateTime(1, 1, 1, airplane.ApplicationTime.Hour, airplane.ApplicationTime.Minute, airplane.ApplicationTime.Second);
                DateTime max = min.AddMinutes(airplane.getRequiredTimeInterval());
                if ((atTime >= min) && (atTime < max))
                    return false;
            }
            return true;
        }

        public bool is_free(DateTime beginTime, DateTime endTime) // индикатор, занята или свободна полоса в указанном интервале
        { // интервал везде представляется как [ ) 
            DateTime change = beginTime;
            while (change != endTime)
            {
                if (!is_free(change))
                    return false;
                change = change.AddMinutes(DataProcessing.modulationStep);
            }
            return true;
        }
    }

    public class Airplane
    {
        public string companyName; // название авиакомпании
        //public int requiredTimeInterval; // необходимый интервал времени, в течение которого самолет занимает полосу
        public bool isArriving; // приезжает, если правда, улетает, если ложь
        public DateTime applicationTime; // время подачи заявки, то есть время, в которое самолет должен занять какую-то полосу
        public string type; // тип самолета, от которого зависит время, которое самолет занимает полосу
        public string flight; // номер рейса, уникальный на все суточное расписание
        public int delta; // отклонение от расписания на сколько минут
        public durations timeIntervals;
        public int runwayNumber; // номер полосы, которую использует самолет
        
        public bool isLonger(Airplane other)
        {
            int left = timeIntervals.arrivalDuration;
            if (!isArriving)
                left = timeIntervals.departureDuration;
            int right = other.timeIntervals.arrivalDuration;
            if (!other.isArriving)
                left = other.timeIntervals.departureDuration;
            if (left > right)
                return true;
            return false;
        }

        public int getRequiredTimeInterval()
        {
            if (isArriving)
                return (timeIntervals.arrivalDuration + DataProcessing.maintenanceTime);
            return (timeIntervals.departureDuration + DataProcessing.maintenanceTime);
        }

        public bool isOnRunway(DateTime now) // занимает ли самолет в текщий момент какую-то полосу
        {
            if ((applicationTime.AddMinutes(Delta) <= now) && (now < applicationTime.AddMinutes(Delta + getRequiredTimeInterval())))
                return true;
            return false;
        }

        public static void generateDelays() // генерирует отклонение самолета от его расписания
        {
            int[] delays = GenerateDelay.get_delays(DataProcessing.airplanes.Count());
            for (int i = 0; i < DataProcessing.airplanes.Count(); i++)
            {
                if (!DataProcessing.airplanes[i].isArriving)
                    DataProcessing.airplanes[i].Delta = delays[i];
                else
                {
                    bool is_minus = DataProcessing.random.Next(0, 2) > 0 ? false : true;
                    if (is_minus)
                        DataProcessing.airplanes[i].Delta = -delays[i];
                    else
                        DataProcessing.airplanes[i].Delta = delays[i];
                }
            }
        }

        public Airplane(DateTime application_time, string flight, string company_name, bool is_arriving, durations required_time_interval)
        {
            ApplicationTime = application_time;
            Flight = flight;
            CompanyName = company_name;
            IsArriving = is_arriving;
            TimeIntervals = required_time_interval;
            Delta = 0;
            runwayNumber = 0;
            type = required_time_interval.type;
        }

        public string CompanyName
        {
            get { return companyName; }
            set { companyName = value; }
        }
        public durations TimeIntervals
        {
            get { return timeIntervals; }
            set { timeIntervals = value; }
        }

        public bool IsArriving
        {
            get { return isArriving; }
            set { isArriving = value; }
        }

        public DateTime ApplicationTime
        {
            get { return applicationTime; }
            set { applicationTime = value; }
        }

        public string Flight
        {
            get { return flight; }
            set { flight = value; }
        }

        public int Delta
        {
            get { return delta; }
            set { delta = value; }
        }

    }

    public static class DataProcessing
    {
        public static int modulationStep;
        public static DateTime currentTime;
        public static int maintenanceTime; // время, необходимое для обслуживае полосы после каждого взлета или посадки
        public static int runwaysAmount;
        public static string scheduleFilename = "SCHEDULE.txt";
        public static string typesWithDurationsFilename = "DURATIONS.txt";
        public static List<durations> timeDurations = new List<durations>();
        public static List<Airplane> airplanes = new List<Airplane>();
        public static Random rnd = new Random();
        public static List<Runway> runways = new List<Runway>();
        public static Random random = new Random();
        public static void readDurations()
        {
            var fileStream = typesWithDurationsFilename;
            using (StreamReader sr = new StreamReader(typesWithDurationsFilename))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] items = line.Split(new char[] { ';' });
                    timeDurations.Add(new durations(items[0], int.Parse(items[1]), int.Parse(items[2])));
                }
            }
        }

        public static void readSchedule()
        {
            var fileStream = scheduleFilename;
            using (StreamReader sr = new StreamReader(scheduleFilename, System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] items = line.Split(new char[] { ';' });
                    DateTime application_time = new DateTime();
                    string[] time = items[0].Split(new char[] { ':' });
                    application_time = application_time.AddHours(int.Parse(time[0]));
                    application_time = application_time.AddMinutes(int.Parse(time[1]));
                    bool is_arriving = true;
                    if (items[4] == "DEPARTURE")
                        is_arriving = false;
                    durations required_time_interval = new durations();
                    foreach (durations t in timeDurations)
                    {
                        if (items[3].StartsWith(t.type))
                            required_time_interval = t;
                    }
                    //items[0] - время; items[1] - номер рейса; items[2] - авиакомпаниия; items[3] - тип самолета; items[4] - отправление или посадка
                    airplanes.Add(new Airplane(application_time, items[1], items[2], is_arriving, required_time_interval));
                }
            }
            Airplane.generateDelays();
        }

        public static bool isOkShedule()
        { // если в какой-то момент времени нужно больше свободных полос, чем существует, то вернет ложь
            DateTime time = currentTime;
            do
            { 
                int count = 0;
                foreach (Airplane airplane in airplanes)
                {
                    if (airplane.isOnRunway(time))
                        count++;
                }
                if (count > runwaysAmount)
                    return false;
                time = time.AddMinutes(modulationStep);
            }
            while ((time.Hour != currentTime.Hour) || (time.Minute != currentTime.Minute));
                return true;
        }

        public static void generateSchedule()
        { // выдает только заведомо выполнимое расписание
            // важными являются только время подачи заявки, тип самолета и уникальный номер рейса
            // сначала выберем количество самолетов того типа, что занимает полосу дольше всех
            List<Airplane> ranges = new List<Airplane>(); 
            DateTime time = currentTime;
            foreach (durations d in timeDurations)
            {
                Airplane plane1 = new Airplane(time, Convert.ToString(ranges.Count()), "", true, d);
                if (ranges.Count() == 0)
                    ranges.Add(plane1);
                else
                {
                    int j = 0;
                    while ((ranges[j].isLonger(plane1))&&(j < ranges.Count()))
                        j++;
                    ranges.Insert(j, plane1);
                }
                Airplane plane2 = new Airplane(time, Convert.ToString(ranges.Count()), "", false, d);
                int i = 0;
                while (ranges[i].isLonger(plane2))
                    i++;
                ranges.Insert(i, plane2);
            }
            // теперь имеем отсортированный по убыванию список, содержащий все различные варианты самолетов
            int free_time = runwaysAmount * 24 * 60;  // 24 часа
            List<Airplane> rand_airplanes = new List<Airplane>();
            foreach (Airplane plane in ranges)
            {
                int max_frequency = free_time / plane.getRequiredTimeInterval();
                int amount = rnd.Next(0, max_frequency); // рандомно решаем, сколько самолетов такого типа будет
                free_time -= amount * plane.getRequiredTimeInterval();
                while (amount > 0)
                {
                    rand_airplanes.Add(plane);
                    amount--;
                }
            } // нашли набор самолетов, с которыми возможно составить рабочее расписание
            // теперь надо растолкать их по полосам

            List<Runway> ways = new List<Runway>(); // имеющиеся полосы
            for (int i = 0; i < runwaysAmount; i++)
                ways.Add(new Runway());

            for (int i = 0; i < rand_airplanes.Count(); i++)
            {
                DateTime begin = new DateTime();
                begin = begin.AddHours(currentTime.Hour);
                begin = begin.AddMinutes(currentTime.Minute);
                DateTime next_day = new DateTime();
                next_day = begin.AddDays(1);
                DateTime end = new DateTime();
                // рандомно ищем временной интервал [begin; end) для данного самолета внутри тех 24 часов, на которых моделируем
                do
                {
                    int mins = (rnd.Next(0, 60*24/GenerateDelay.integral_step)) * GenerateDelay.integral_step; // домножаем на шаг, который есть минимальная единица измерения времени в программе (5 минут)
                   if (mins != 0)
                        begin = begin.AddMinutes(mins);
                    end = begin.AddMinutes(rand_airplanes[i].getRequiredTimeInterval());
                } while (end >= next_day);

                // с end до begin находится нужный временной интервал для rand_airplanes
                for (int j = 0; j < runwaysAmount; j++)
                {
                    if (ways[j].airplanesUse.Count == 0)
                    {
                        ways[j].airplanesUse.Add(rand_airplanes[i]);
                        rand_airplanes[i].applicationTime = begin;
                        rand_airplanes[i].runwayNumber = j + 1;
                        airplanes.Add(rand_airplanes[i]);
                        break;
                    }
                    if (ways[j].is_free(begin, end))
                    {
                        ways[j].airplanesUse.Add(rand_airplanes[i]);
                        rand_airplanes[i].applicationTime = begin;
                        rand_airplanes[i].runwayNumber = j + 1;
                        airplanes.Add(rand_airplanes[i]);
                        break;
                    }
                    if (j == runwaysAmount - 1)
                    {
                        begin = begin.AddMinutes(modulationStep);
                        end = end.AddMinutes(modulationStep);
                        j = 0;
                        if (end >= next_day)
                            break;
                    }
                }
            }
            
            using (StreamWriter wr = new StreamWriter(scheduleFilename))
            {
                foreach (Airplane airplane in airplanes)
                {
                    string status = "DEPARTURE";
                    if (airplane.isArriving)
                        status = "ARRIVAL";
                    wr.WriteLine("{0};{1};{2};{3};{4}", airplane.applicationTime.ToShortTimeString(), airplane.flight, airplane.companyName, airplane.type, status);
                }
            }
        }
    }
}
