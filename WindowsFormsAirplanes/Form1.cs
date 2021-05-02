using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using KursovayaOOP_f;
using System.Diagnostics;

namespace WindowsFormsAirplanes
{
    public partial class FormRunwayController : Form
    {
        private static bool comboBoxModulationMode_selected = false;
        private static bool comboBoxModulationStep_selected = false;
        private static bool comboBoxRunwayMaintenanceTime_selected = false;
        private static bool comboBoxModulationStartTimeMinutes_selected = false;
        private static bool comboBoxModulationStartTimeHours_selected = false;
        private static bool comboBoxAmountOfRunways_selected = false;
        private static bool buttonTypesWithDurations_clicked = false;
        private static bool buttonGenerateSchedule_clicked = false;
        private static bool buttonManualShedule_clicked = false;
        private static bool buttonStart_clicked = false;
        private static bool modulationEnded = false;
        public FormRunwayController()
        {
            InitializeComponent();
            // запрещаем нажимать кнопки, разрешаем после того как нужная информация будет заполнена
            buttonManualShedule.Enabled = false;
            buttonTypesWithDurations.Enabled = false;
            //buttonAirplanesWithTypes.Enabled = false;
            buttonGenerateSchedule.Enabled = false;
            buttonStart.Enabled = false;
            buttonNextStep.Enabled = false;
            DataProcessing.currentTime = new DateTime(2, 1, 1);
        }

        public void changeVisibleButtons()
        {
            if (comboBoxModulationMode_selected && comboBoxModulationStep_selected && comboBoxRunwayMaintenanceTime_selected && comboBoxModulationStartTimeMinutes_selected && comboBoxModulationStartTimeHours_selected && comboBoxAmountOfRunways_selected)
                buttonTypesWithDurations.Enabled = true;
            if (buttonTypesWithDurations_clicked)
            {
                comboBoxModulationMode.Enabled = false;
                comboBoxModulationStartTimeMinutes.Enabled = false;
                comboBoxModulationStartTimeHours.Enabled = false;
                comboBoxAmountOfRunways.Enabled = false;
                comboBoxModulationStep.Enabled = false;
                comboBoxRunwayMaintenanceTime.Enabled = false;
                buttonGenerateSchedule.Enabled = true;
                buttonManualShedule.Enabled = true;
            }
            if (buttonGenerateSchedule_clicked)
            {
                buttonTypesWithDurations.Enabled = false;
                buttonManualShedule.Enabled = false;
                buttonStart.Enabled = true;
            }
            if (buttonManualShedule_clicked)
            {
                buttonTypesWithDurations.Enabled = false;
                buttonGenerateSchedule.Enabled = false;
                buttonStart.Enabled = true;
            }
            if (buttonStart_clicked)
            {
                buttonStart.Enabled = false;
                if (comboBoxModulationMode.SelectedIndex == 1)
                    buttonNextStep.Enabled = true;
                buttonGenerateSchedule.Enabled = false;
                buttonManualShedule.Enabled = false;
            }
            else
                buttonNextStep.Enabled = false;
            if (modulationEnded)
            {
                buttonNextStep.Enabled = false;
                timerNextStep.Enabled = false;
            }
        }

        private void comboBoxRunwayMaintenanceTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxRunwayMaintenanceTime_selected = true;
            DataProcessing.maintenanceTime = int.Parse(comboBoxRunwayMaintenanceTime.Text);
            changeVisibleButtons();
        }

        private void buttonManualShedule_Click(object sender, EventArgs e)
        {
            if (openFileDialogSchedule.ShowDialog() == DialogResult.Cancel)
                return;
            openFileDialogSchedule.Filter = "Text files(*.txt)|*.txt";
            DataProcessing.scheduleFilename = openFileDialogSchedule.FileName;
            buttonManualShedule_clicked = true;
            changeVisibleButtons();

        }

        private void comboBoxModulationStep_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxModulationStep_selected = true;
            DataProcessing.modulationStep = int.Parse(comboBoxModulationStep.Text);
            changeVisibleButtons();
        }

        private void fill_runways_page()
        {
            dataGridViewRunways.Rows.Clear();
            foreach (KeyValuePair<int, Runway> runway in DataProcessing.runways)
            {
                string[] way = { "0", "1", "" };
                way[0] = runway.Key.ToString();
                if (runway.Value.countAirplanes() == 0)
                    way[1] = "free";
                else
                {
                    Airplane now_plane = runway.Value.usingAirplane(DataProcessing.currentTime);
                    if (now_plane.runwayNumber == 0)
                        way[1] = "free";
                    else
                    {
                        if (!runway.Value.is_free(DataProcessing.currentTime))
                        {
                            way[1] = "in use";
                            way[2] = now_plane.flight;
                            if (DataProcessing.currentTime >= now_plane.applicationTime.AddMinutes(now_plane.getRequiredTimeInterval() - DataProcessing.maintenanceTime))
                            {
                                way[1] = "maintenance";
                                way[2] = "after " + way[2];
                            }
                        }
                        else
                            way[1] = "free";
                    }
                }
                dataGridViewRunways.Rows.Add(way);
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            labelNowTime.Text = DataProcessing.currentTime.ToShortTimeString();
            buttonStart_clicked = true;
            changeVisibleButtons();
            DataProcessing.readDurations();
            if (buttonGenerateSchedule_clicked)
            {
                DataProcessing.generateSchedule();
                DataProcessing.init_correct();
                fill_separeted_schedules();
                fill_runways_page();
                fill_queues();
                if (comboBoxModulationMode.SelectedIndex == 0)
                {
                    timerNextStep.Interval = 3000; // in milliseconds
                    timerNextStep.Enabled = true;
                   // timerNextStep.Tick += new EventHandler(timerNextStep_Tick);
                }
            }
            else if (buttonManualShedule_clicked)
            {
                DataProcessing.readSchedule();
                if (!DataProcessing.isOkShedule())
                {
                    MessageBox.Show("The schedule is impracticable, it needs changing", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    buttonManualShedule_clicked = false;
                    buttonTypesWithDurations.Enabled = false;
                    buttonStart_clicked = false;
                    changeVisibleButtons();
                }
                else
                {
                    DataProcessing.init_correct();
                    fill_separeted_schedules();
                    fill_runways_page();
                    fill_queues();
                    if (comboBoxModulationMode.SelectedIndex == 0)
                    {
                        timerNextStep.Interval = 3000; // in milliseconds
                        timerNextStep.Enabled = true;
                        //timerNextStep.Tick += new EventHandler(timerNextStep_Tick);
                    }
                }
            }
            
        }

        public void fill_separeted_schedules()
        {
            dataGridViewArrivalSchedule.Rows.Clear();
            dataGridViewDepartureSchedule.Rows.Clear();
            // надо составить сортированный по времени лист из самолетов на полосах и из самолетов в очереди

            foreach (Airplane airplane in DataProcessing.airplanes)
            {
                string status = "";
                
                DateTime applTime = DataProcessing.find_in_runways(airplane.flight);
                if (applTime != new DateTime())
                {
                    int real_delta = (int)applTime.Subtract(airplane.applicationTime).TotalMinutes;
                    if ((DataProcessing.currentTime >= applTime) && (DataProcessing.currentTime < applTime.AddMinutes(airplane.getRequiredTimeInterval() - DataProcessing.maintenanceTime)))
                    { // самолет сейчас на полосе, возможно, идет время обслуживания
                        status += "now ";
                        //if (DataProcessing.currentTime >= applTime.AddMinutes(airplane.getRequiredTimeInterval() - DataProcessing.maintenanceTime))
                        //{
                        //    status = "maintenance ";
                        //}
                        if (real_delta != 0)
                            status += " with delta = " + real_delta.ToString();
                    }
                    else
                    {
                        if (DataProcessing.currentTime < applTime)
                        {
                            if (real_delta == 0)
                                status += "will be at time";
                            else
                                status += "will be with delta = " + real_delta.ToString();
                        }
                        else
                        {
                            if (real_delta == 0)
                                status += "was at time";
                            else
                                status += "was with delta = " + real_delta.ToString();
                        }
                    }
                }
                else
                { // тогда рейс в очереди на посадку/взлет
                    if (airplane.delta > 0)
                        status = "is later for " + airplane.delta.ToString() + ", waiting";
                    else if (airplane.delta < 0)
                        status = "is earlier for " + (-airplane.delta).ToString() + ", waiting";
                    else
                        status = "in queue "; // никогда не должно являться

                }
                string[] plane = { airplane.applicationTime.ToShortTimeString(), airplane.flight, airplane.companyName, airplane.type, status };
                if (airplane.isArriving)
                {
                    plane[4] = "arriving " + plane[4];
                    dataGridViewArrivalSchedule.Rows.Add(plane);
                }
                else
                {
                    plane[4] = "departuring " + plane[4];
                    dataGridViewDepartureSchedule.Rows.Add(plane);
                }
            }

        }

        public void fill_queues()
        {
            dataGridViewDepartureQueue.Rows.Clear();
            dataGridViewArrivalQueue.Rows.Clear();
            string[] plane = new string[3];
            foreach (Airplane airplane in DataProcessing.arrival_queue)
            {
                plane[0] = airplane.applicationTime.ToShortTimeString();
                plane[1] = airplane.flight;
                plane[2] = airplane.delta.ToString();
                if (airplane.isArriving)
                    dataGridViewArrivalQueue.Rows.Add(plane);
                else
                    dataGridViewDepartureQueue.Rows.Add(plane);
            }
        }













        
        
        private void buttonTypesWithDurations_Click(object sender, EventArgs e)
        {
            
            if (openFileDialogTypesWithDurations.ShowDialog() == DialogResult.Cancel)
                return;// какого-то черта застревает на этом ифе и все
            openFileDialogTypesWithDurations.Filter = "Text files(*.txt)|*.txt";
            DataProcessing.typesWithDurationsFilename = openFileDialogTypesWithDurations.FileName;
            buttonTypesWithDurations_clicked = true;
            changeVisibleButtons(); 
        }

        private void buttonGenerateSchedule_Click(object sender, EventArgs e)
        {
            buttonGenerateSchedule_clicked = true;
            changeVisibleButtons();
        }

        private void comboBoxAmountOfRunways_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxAmountOfRunways_selected = true;
            DataProcessing.runwaysAmount = int.Parse(comboBoxAmountOfRunways.Text);
            changeVisibleButtons();
        }

        private void comboBoxModulationStartTimeHours_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxModulationStartTimeHours_selected = true;
            DataProcessing.currentTime = DataProcessing.currentTime.AddHours(int.Parse(comboBoxModulationStartTimeHours.Text));
            changeVisibleButtons();
        }

        private void comboBoxModulationStartTimeMinutes_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxModulationStartTimeMinutes_selected = true;
            DataProcessing.currentTime = DataProcessing.currentTime.AddMinutes(int.Parse(comboBoxModulationStartTimeMinutes.Text));
            changeVisibleButtons();
        }

        private void comboBoxModulationMode_SelectedIndexChanged(object sender, EventArgs e)
        {

            comboBoxModulationMode_selected = true;
            changeVisibleButtons();
        }

        private void FormRunwayController_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
           // this.Show();
        }

        private void buttonNextStep_Click(object sender, EventArgs e)
        {
            DataProcessing.currentTime = DataProcessing.currentTime.AddMinutes(DataProcessing.modulationStep);
            labelNowTime.Text = DataProcessing.currentTime.ToShortTimeString();
            DataProcessing.correctDeltas();
            fill_separeted_schedules();
            fill_runways_page();
            fill_queues();
            for (int i = 0; i < DataProcessing.arrival_queue.Count; i++)
            {
                DataProcessing.arrival_queue[i].delta += DataProcessing.modulationStep;
                DataProcessing.airplanes[DataProcessing.find_airplane_index(DataProcessing.arrival_queue[i].flight)].delta = DataProcessing.arrival_queue[i].delta;
            }
            if ((int.Parse(comboBoxModulationStartTimeHours.Text) == DataProcessing.currentTime.Hour) && (int.Parse(comboBoxModulationStartTimeMinutes.Text) == DataProcessing.currentTime.Minute))
            {
                modulationEnded = true;
                buttonNextStep.Text = "modulation ended";
                changeVisibleButtons();
            }

        }

        private void timerNextStep_Tick(object sender, EventArgs e)
        {
            buttonNextStep_Click(sender, e);
        }

        private void buttonHelp_Click(object sender, EventArgs e)
        {
            Process.Start("HELP.txt");
        }
    }
}
