using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KursovayaOOP_f
{
    public struct durations
    {// содержит тип самолета, от которого зависят продолжительность взлета и посадки, считывается из файла
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
    { // полоса - содержит отсортированные по времени самолеты, которые ее используют
        private SortedList<DateTime, Airplane> airplanesUse; 
        public Runway()
        {
            airplanesUse = new SortedList<DateTime, Airplane>();
        }
        // на полосах все дельты = 0 и они изменяются тогда, когда система о них узнает
        // все дельты прописаны в DataProcessing.airplanes


        public DateTime find_on_flight(string a_flight)
        {
            foreach (KeyValuePair<DateTime, Airplane> plane in airplanesUse)
            {
                if (plane.Value.flight == a_flight)
                    return plane.Key;
            }
            return new DateTime();
        }

        public List<KeyValuePair<DateTime, DateTime>> get_empties()
        { // находит и возвращает свободные временные интервалы полосы начиная с момента currentTime
            List < KeyValuePair<DateTime, DateTime> > empties = new List<KeyValuePair<DateTime, DateTime>>();
            DateTime begin, end;
            for (int i = 0; i < airplanesUse.Keys.Count - 1; i++)
            {
                begin = airplanesUse[airplanesUse.Keys[i]].applicationTime.AddMinutes(airplanesUse[airplanesUse.Keys[i]].getRequiredTimeInterval());
                if (begin < DataProcessing.currentTime)
                    begin = DataProcessing.currentTime;
                end = airplanesUse[airplanesUse.Keys[i + 1]].applicationTime;
                if (begin < end)  
                    empties.Add(new KeyValuePair<DateTime, DateTime>(begin, end));
            }
            if (airplanesUse.Count > 0)
            { 
                begin = airplanesUse[airplanesUse.Keys[airplanesUse.Keys.Count - 1]].applicationTime.AddMinutes(airplanesUse[airplanesUse.Keys[airplanesUse.Keys.Count - 1]].getRequiredTimeInterval());
                end = airplanesUse[airplanesUse.Keys[0]].applicationTime.AddDays(1);
                empties.Add(new KeyValuePair<DateTime, DateTime>(begin, end));
            }
            return empties;
        }

        public bool next_use(ref DateTime before)
        {
            foreach (DateTime key in airplanesUse.Keys)
            {
                if (key > before)
                {
                    before = key;
                    return true;
                }
            }
            return false;
        }
        public void erase_plane(DateTime time)
        {
            this.airplanesUse.Remove(time);
        }

        public List<Airplane> get_airplanes(DateTime begin, DateTime end)
        { // возвращает список самолетов, полностью влезающих с учетом дельты в данный интервал 
            List<Airplane> airplanes = new List<Airplane>();

            foreach (KeyValuePair<DateTime, Airplane> plane in airplanesUse)
            {
                if ((plane.Key.AddMinutes(plane.Value.delta) >= begin) && (plane.Key.AddMinutes(plane.Value.delta + plane.Value.getRequiredTimeInterval()) < end))
                {
                    airplanes.Add(plane.Value);
                }
            }
            return airplanes;
        }

        public void add_delta(DateTime time, int new_delta)
        {
            airplanesUse[time].delta += new_delta;
        }
        public int countAirplanes()
        { // посчитать количество самолетов на полосе
            return this.airplanesUse.Count;
        }

        public void addAirplane(DateTime time, Airplane airplane)
        { // добавить самолет на полосу с указанием времени заявки
            this.airplanesUse.Add(time, airplane);
        }

        public Airplane usingAirplane(DateTime time)
        { // узнать самолет, использовавший полосу последним относительно данного time момента времени
            Airplane airplane = new Airplane(new DateTime(2, 1, 1), "", "", true, new durations(), 0);
            foreach(KeyValuePair<DateTime, Airplane> plane in airplanesUse)
            {
                if (plane.Value.applicationTime <= time)
                    airplane = new Airplane(plane.Value.applicationTime, plane.Value.flight, plane.Value.companyName, plane.Value.isArriving, plane.Value.timeIntervals, plane.Value.runwayNumber);
                else
                    break;
            }
            return airplane;
        }

        public bool is_free(DateTime atTime) // индикатор, занята или свободна полоса в момент atTime
        {
            foreach (KeyValuePair<DateTime, Airplane> airplane in airplanesUse)
            {
                // если в текущее время на этой полосе самолетный интервал, то занята (учитывается время технического обслуживания полосы)
                DateTime min = new DateTime(2, 1, airplane.Value.applicationTime.Day, airplane.Value.applicationTime.Hour, airplane.Value.applicationTime.Minute, airplane.Value.applicationTime.Second);
                min = min.AddMinutes(airplane.Value.delta);
                DateTime max = min.AddMinutes(airplane.Value.getRequiredTimeInterval());
                if ((atTime >= min) && (atTime < max))
                    return false;
            }
            return true;
        }

        public bool is_free(DateTime beginTime, DateTime endTime) // индикатор, занята или свободна полоса в указанном интервале
        { // интервал везде представляется как [ ) 
            // если в какой-то момент времени из данного интервала полоса была занята, вернет false
            DateTime change = new DateTime(2, 1, beginTime.Day, beginTime.Hour, beginTime.Minute, 0, 0);
            while (change < endTime)
            {
                if (!is_free(change))
                    return false;
                change = change.AddMinutes(GenerateDelay.integral_step);//(DataProcessing.modulationStep);
            }
            return true;
        }
    }

    public class Airplane
    {
        public string companyName; // название авиакомпании
        public bool isArriving; // приезжает, если правда, улетает, если ложь
        public DateTime applicationTime; // время подачи заявки, то есть время, в которое самолет должен занять какую-то полосу
        public string type; // тип самолета, от которого зависит время, которое самолет занимает полосу
        public string flight; // номер рейса, уникальный на все суточное расписание
        public int delta; // отклонение от расписания на сколько минут
        public durations timeIntervals;
        public int runwayNumber; // номер полосы, которую использует самолет

        public bool isLonger(Airplane other)
        { // сравнивает текущий самолет с данным по тому времени, которое они занимают на полосе, возвращает правду, если первый дольше второго
            int left = timeIntervals.arrivalDuration;
            if (!isArriving)
                left = timeIntervals.departureDuration;
            int right = other.timeIntervals.arrivalDuration;
            if (!other.isArriving)
                right = other.timeIntervals.departureDuration;
            if (left > right)
                return true;
            return false;
        }

        public int getRequiredTimeInterval()
        { // возвращает время, нужное на посадку + время для обслуживания полосы для текущего самолета
            if (isArriving)
                return (timeIntervals.arrivalDuration + DataProcessing.maintenanceTime);
            return (timeIntervals.departureDuration + DataProcessing.maintenanceTime);
        }

        public bool isOnRunway(DateTime now) // занимает ли самолет в текщий момент какую-то полосу
        {
            if ((applicationTime.AddMinutes(delta) <= now) && (now < applicationTime.AddMinutes(delta + getRequiredTimeInterval())))
                return true;
            return false;
        }

        public static void generateDelays() // генерирует отклонение самолета от его расписания
        {
            int[] delays = GenerateDelay.get_delays(DataProcessing.airplanes.Count());
            for (int i = 0; i < DataProcessing.airplanes.Count(); i++)
            {
                if (!DataProcessing.airplanes[i].isArriving)
                    DataProcessing.airplanes[i].delta = delays[i];
                else
                {
                    bool is_minus = DataProcessing.rnd.Next(0, 2) > 0 ? false : true;
                    if (is_minus)
                        DataProcessing.airplanes[i].delta = -delays[i];
                    else
                        DataProcessing.airplanes[i].delta = delays[i];
                }
            }
        }

        public Airplane(DateTime application_time, string _flight, string company_name, bool is_arriving, durations required_time_interval, int runway)
        {
            applicationTime = application_time;
            flight = _flight;
            companyName = company_name;
            isArriving = is_arriving;
            timeIntervals = required_time_interval;
            delta = 0;
            runwayNumber = runway;
            type = required_time_interval.type;
        }

        public static bool operator >(Airplane one, Airplane two)
        {
            return (one.applicationTime > two.applicationTime);
        }
        public static bool operator <(Airplane one, Airplane two)
        {
            return (one.applicationTime < two.applicationTime);
        }
    }

    public static class DataProcessing
    {
        public static int modulationStep; // шаг моделирования
        public static DateTime currentTime = new DateTime(2,1,1,0,0,0,0);
        public static int maintenanceTime; // время, необходимое для обслуживае полосы после каждого взлета или посадки
        public static int runwaysAmount; // количество ВПП
        public static string scheduleFilename = "SCHEDULE.txt";
        public static string typesWithDurationsFilename = "DURATIONS.txt";
        public static List<durations> timeDurations = new List<durations>(); // все возможные типы со временем, нужным для взлета или посадки
        public static List<Airplane> airplanes = new List<Airplane>(); // все самолеты из расписания
        public static Random rnd = new Random();
        public static SortedList<int, Runway> runways = new SortedList<int, Runway>(); // полосы, содержащие принадлежащие им самолеты
        //public static DateTime lastCheck = new DateTime(2,1,1,0,0,0,0);
        public static List<Airplane> arrival_queue = new List<Airplane>();
        public static Queue<Airplane> departure_queue = new Queue<Airplane>();


        public static int find_airplane_index(string a_flight)
        {
            for (int i = 0; i < airplanes.Count; i++)
            {
                if (airplanes[i].flight == a_flight)
                    return i;
            }
            return -10;
        }

        public static int find_in_queue(string a_flight)
        {
            if (arrival_queue.Count != 0)
            {
                for (int i = 0; i < arrival_queue.Count; i++)
                {
                    if (airplanes[i].flight == a_flight)
                        return i;
                }
            }
            return -11; // возвращает в случае ненахождения рейса среди ожидающих
        }

        public static DateTime find_in_runways(string a_flight)
        {
            DateTime time = new DateTime();
            foreach (KeyValuePair<int, Runway> pair in runways)
            {
                if (pair.Value.countAirplanes() != 0)
                {
                    time = pair.Value.find_on_flight(a_flight);
                    if (time != new DateTime())
                        break;
                }
            }
            return time; // возвращает applicationTime рейса, если он принадлежит какой-то полосе
            // возвращает new DateTime() если не был найден на полосах
        }

        public static int get_integral_step()
        {
            return GenerateDelay.integral_step;
        }

        public static void init_correct()
        {
            //это самое начало, такое один раз бывает, поэтому скорректируем все отрицательные дельты
            for (int i = 0; i < airplanes.Count; i++)
            {
                if (airplanes[i].delta < 0)
                    airplanes[i].delta -= airplanes[i].delta % modulationStep;
                // блин хорошо бы это в виде ожидания впихнуть в статистику...
            }
            DateTime limit = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, currentTime.Second);
            limit = limit.AddHours(2); // считаем, что узнаем обо всех задержках за 2 часа
            limit = limit.AddMinutes(-modulationStep);
            for (int i = 0; i < airplanes.Count; i++)
            {
                if ((airplanes[i].applicationTime < limit) && (airplanes[i].delta > 0))
                { // надо перенести дельту отсюда в соответствующий самолет на полосе
                    runways[airplanes[i].runwayNumber].erase_plane(airplanes[i].applicationTime);
                    arrival_queue.Add(airplanes[i]);
                }
            }
        }

        public static void correctDeltas()
        {
            for (int i = 0; i < airplanes.Count; i++)
            {
                if ((currentTime.AddMinutes(120 - modulationStep) <= airplanes[i].applicationTime) && ((currentTime.AddHours(2) > airplanes[i].applicationTime)))
                {
                    if (airplanes[i].delta > 0)
                    {
                        runways[airplanes[i].runwayNumber].erase_plane(airplanes[i].applicationTime);
                        arrival_queue.Add(airplanes[i]);
                    }
                }
                else if ((airplanes[i].applicationTime.AddMinutes(airplanes[i].delta) == currentTime) && (airplanes[i].delta < 0))
                {
                    runways[airplanes[i].runwayNumber].erase_plane(airplanes[i].applicationTime);
                    if (arrival_queue.Count == 0)
                        arrival_queue.Add(airplanes[i]);
                    else
                    {
                        int index = 0;
                        while (arrival_queue[index].delta < 0)
                        {
                            index++;
                            if (index == arrival_queue.Count)
                                break;
                        }
                        arrival_queue.Insert(index, airplanes[i]);
                    }
                }
            }
            // на полосах останутся только те, у кого нет задержек или о чьих задержках еще неизвестно

            // все, чего-то ожидающие, стоят в очереди. Прилетевшие раньше стоят в этой очереди первыми
            // в порядке появления, то есть выходит очередь с приоритетами 
            // теперь мы будем идти по всей очереди, пытаясь впихнуть 
            // сначала смотрим все досрочно прилетевшие и если сейчас их applicationTime то сажаем их как по расписанию
            // далее смотрим уже всю очередь по порядку, пытаясь впихнуть из них того, кто влезет, но самый длинный из возможных
            if (arrival_queue.Count != 0)
            { // сажаем самолеты из очереди, которые дождались до того, что наступил их интервал по расписанию
                for (int i = 0; i < arrival_queue.Count; i++)
                {
                    if (arrival_queue[i].delta <= 0) // !!!!!!
                    {
                        if (currentTime == arrival_queue[i].applicationTime)
                        {
                            Airplane airplane = new Airplane(arrival_queue[i].applicationTime, arrival_queue[i].flight, arrival_queue[i].companyName, arrival_queue[i].isArriving, arrival_queue[i].timeIntervals, arrival_queue[i].runwayNumber);
                            runways[arrival_queue[i].runwayNumber].addAirplane(airplane.applicationTime, airplane);
                            arrival_queue.RemoveAt(i);
                            i--;
                        }
                    }
                    else
                        break;
                }
            }
            if (arrival_queue.Count != 0)
            { // сажаем все самолеты из очереди, которые можем
                foreach (KeyValuePair<int, Runway> runway in runways)
                {
                    List<KeyValuePair<DateTime, DateTime>> empties = runway.Value.get_empties();
                    if (empties.Count != 0)
                    {
                        foreach (KeyValuePair<DateTime, DateTime> interval in empties)
                        {
                            int adding_index = -1;
                            for (int i = 0; i < arrival_queue.Count; i++)
                            {
                                if ((arrival_queue[i].applicationTime.AddMinutes(arrival_queue[i].delta) >= interval.Key) && (arrival_queue[i].applicationTime.AddMinutes(arrival_queue[i].delta + arrival_queue[i].getRequiredTimeInterval()) <= interval.Value))
                                {
                                    adding_index = i;
                                }
                            }
                            if (adding_index != -1)
                            {
                                Airplane airplane = new Airplane(arrival_queue[adding_index].applicationTime.AddMinutes(arrival_queue[adding_index].delta), arrival_queue[adding_index].flight, arrival_queue[adding_index].companyName, arrival_queue[adding_index].isArriving, arrival_queue[adding_index].timeIntervals, runway.Key);
                                runway.Value.addAirplane(airplane.applicationTime, airplane);
                                arrival_queue.RemoveAt(adding_index);
                            }
                        }
                    }
                }
            }

            // теперь надо проследить за увеличением дельт
            // у всех, кто находится в очереди, дельта увеличивается на то, насколько его время отправки с учетом дельты отличается от текущего времени

            //for (int i = 0; i < arrival_queue.Count; i++)
            //{
            //    arrival_queue[i].delta += modulationStep; //(int)currentTime.Subtract(arrival_queue[i].applicationTime).TotalMinutes;
            //    airplanes[find_airplane_index(arrival_queue[i].flight)].delta = arrival_queue[i].delta;
            //}
   
        }

        private static int CompareAirplanesForSchedule(Airplane one, Airplane two)
        {
            if (one.applicationTime == two.applicationTime)
                return 0;
            if (one.applicationTime > two.applicationTime)
                return 1;
            else
                return -1;
        }
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
            runways.Clear();
            airplanes.Clear();
            var fileStream = scheduleFilename;
            using (StreamReader sr = new StreamReader(scheduleFilename, System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] items = line.Split(new char[] { ';' });
                    DateTime application_time = new DateTime(2, 1, 1);
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
                    airplanes.Add(new Airplane(application_time, items[1], items[2], is_arriving, required_time_interval, -1));
                }
            }
            airplanes.Sort(CompareAirplanesForSchedule);
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
                time = time.AddMinutes(GenerateDelay.integral_step);
            }
            while (time < currentTime.AddDays(1));
            // тут надо сразу распихать по полосам самолеты, они отсортированы по времени
            for (int i = 1; i <= runwaysAmount; i++)
                runways.Add(i, new Runway());
            foreach (Airplane plane in airplanes)
            {
                DateTime begin = new DateTime(2, 1, plane.applicationTime.Day, plane.applicationTime.Hour, plane.applicationTime.Minute, 0, 0);
                DateTime end = begin.AddMinutes(plane.getRequiredTimeInterval());
                for (int i = 1; i <= runwaysAmount; i++)
                {
                    if (runways[i].countAirplanes() == 0)
                    {
                        Airplane now_plane = new Airplane(begin, plane.flight, plane.companyName, plane.isArriving, plane.timeIntervals, i);
                        now_plane.runwayNumber = i;
                        plane.runwayNumber = i;
                        runways[i].addAirplane(begin, now_plane);
                        break;
                    }
                    else if (runways[i].is_free(begin, end))
                    {
                        Airplane now_plane = new Airplane(begin, plane.flight, plane.companyName, plane.isArriving, plane.timeIntervals, i);
                        now_plane.runwayNumber = i;
                        plane.runwayNumber = i;
                        runways[i].addAirplane(begin, now_plane);
                        break;
                    }
                }
            }
            Airplane.generateDelays();
            return true;
        }

        public static void generateSchedule()
        { // выдает только заведомо выполнимое расписание
            // важными являются только время подачи заявки, тип самолета и уникальный номер рейса
            // сначала выберем количество самолетов того типа, что занимает полосу дольше всех
            runways.Clear();
            airplanes.Clear();
            List<Airplane> ranges = new List<Airplane>(); 
            DateTime time = currentTime;
            foreach (durations d in timeDurations)
            {
                Airplane plane1 = new Airplane(time, Convert.ToString(ranges.Count()), "A_Airlines", true, d, -2);
                if (ranges.Count() == 0)
                    ranges.Add(plane1);
                else
                {
                    int j = 0;
                    while ((ranges[j].isLonger(plane1))&&(j < ranges.Count()))
                        j++;
                    ranges.Insert(j, plane1);
                }
                Airplane plane2 = new Airplane(time, Convert.ToString(ranges.Count()), "D_Airlines", false, d, -3);
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
                int amount = rnd.Next(0, max_frequency + 1); // рандомно решаем, сколько самолетов такого типа будет
                free_time -= amount * plane.getRequiredTimeInterval();
                while (amount > 0)
                {
                    rand_airplanes.Add(plane);
                    amount--;
                }
            } // нашли набор самолетов, с которыми возможно составить рабочее расписание
            // теперь надо растолкать их по полосам

            for (int i = 1; i <= runwaysAmount; i++)
                runways.Add(i, new Runway());

            for (int i = 0; i < rand_airplanes.Count(); i++)
            {
                DateTime begin = new DateTime(2, 1, 1, currentTime.Hour, currentTime.Minute, 0, 0);
                DateTime next_day = new DateTime(2, 1, 2, currentTime.Hour, currentTime.Minute, 0, 0);
                DateTime end;
                // рандомно ищем временной интервал [begin; end) для данного самолета внутри тех 24 часов, на которых моделируем
                do
                {
                    int mins = (rnd.Next(0, 60*24/GenerateDelay.integral_step)) * GenerateDelay.integral_step; // домножаем на шаг, который есть минимальная единица измерения времени в программе (5 минут)
                    if (mins != 0)
                    {
                        begin = new DateTime(2, 1, 1, currentTime.Hour, currentTime.Minute, 0, 0);
                        begin = begin.AddMinutes(mins);
                    }    
                    end = begin.AddMinutes(rand_airplanes[i].getRequiredTimeInterval());
                } while (end >= next_day);

                // с end до begin находится нужный временной интервал для rand_airplanes
                for (int j = 1; j <= runwaysAmount; j++)
                {
                    if (runways[j].countAirplanes() == 0)
                    {
                        Airplane now_plane = new Airplane(begin, "SB " + rand_airplanes[i].flight + i.ToString() + j.ToString(), rand_airplanes[i].companyName, rand_airplanes[i].isArriving, rand_airplanes[i].timeIntervals, j);
                        now_plane.runwayNumber = j;
                        airplanes.Add(now_plane);
                        runways[j].addAirplane(begin, now_plane);
                        break;
                    }
                    if (runways[j].is_free(begin, end))
                    {
                        Airplane now_plane = new Airplane(begin, "SB " + rand_airplanes[i].flight + i.ToString() + j.ToString(), rand_airplanes[i].companyName, rand_airplanes[i].isArriving, rand_airplanes[i].timeIntervals, j);
                        now_plane.runwayNumber = j;
                        airplanes.Add(now_plane);
                        runways[j].addAirplane(begin, now_plane);
                        break;
                    }
                    if (j == runwaysAmount)
                    {
                        begin = begin.AddMinutes(GenerateDelay.integral_step);
                        begin = new DateTime(2, 1, begin.Day, begin.Hour, begin.Minute, 0, 0);
                        end = end.AddMinutes(GenerateDelay.integral_step);
                        j = 0;
                        if (end >= next_day)
                            break;
                    }
                }
            }
            airplanes.Sort(CompareAirplanesForSchedule);

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
            Airplane.generateDelays();
        }
    }
}
