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
            DataProcessing.currentTime = new DateTime();
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
                buttonNextStep.Enabled = false;
        }

        private void FormRunwayController_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxRunwayMaintenanceTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxRunwayMaintenanceTime_selected = true;
            DataProcessing.maintenanceTime = int.Parse(comboBoxRunwayMaintenanceTime.Text);
            changeVisibleButtons();
        }

        private void labelRunwayMaintenanceTime_Click(object sender, EventArgs e)
        {
            
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

        private void buttonStart_Click(object sender, EventArgs e)
        {
            labelNowTime.Text = DataProcessing.currentTime.ToShortTimeString();
            buttonStart_clicked = true;
            changeVisibleButtons();
            DataProcessing.readDurations();
            if (buttonGenerateSchedule_clicked)
            {
                DataProcessing.generateSchedule();  
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
                    fill_separeted_schedules();
                }
            }
            
        }

        public void fill_separeted_schedules()
        {
            dataGridViewArrivalSchedule.Rows.Clear();
            dataGridViewDepartureSchedule.Rows.Clear();
            foreach (Airplane airplane in DataProcessing.airplanes)
            {
                if (airplane.IsArriving)
                {
                    string status;
                    if ((DataProcessing.currentTime >= airplane.ApplicationTime.AddMinutes(airplane.Delta))&&(DataProcessing.currentTime < airplane.ApplicationTime.AddMinutes(airplane.Delta + airplane.timeIntervals.arrivalDuration))) // учитывает время обслуживания полосы после приземления самолета как время его посадки
                    {
                        status = "now landing";
                        if (airplane.Delta < 0)
                            status = "now landing, " + (-airplane.Delta).ToString() + " min earlier";
                        else if (airplane.Delta > 0)
                            status = "now landing, " + (airplane.Delta).ToString() + " min later";
                    }
                    else if (airplane.applicationTime.AddMinutes(airplane.Delta) < DataProcessing.currentTime)
                    {
                        status = "landed on time";
                        if (airplane.Delta > 0)
                            status = "landed " + airplane.Delta.ToString() + " min later";
                        else if (airplane.Delta < 0)
                            status = "landed " + (-airplane.Delta).ToString() + " min earlier";
                    }
                    else
                    {
                        status = "should land on time";
                        if (airplane.Delta > 0)
                            status = "arrival is delayed for " + airplane.Delta.ToString() + " min";
                    }
                    string[] plane = { airplane.applicationTime.ToShortTimeString(), airplane.Flight, airplane.CompanyName, airplane.type,  status};
                    dataGridViewArrivalSchedule.Rows.Add(plane);
                }
                else 
                {
                    string status;
                    if ((DataProcessing.currentTime >= airplane.ApplicationTime.AddMinutes(airplane.Delta)) && (airplane.ApplicationTime.AddMinutes(airplane.Delta + airplane.timeIntervals.departureDuration) > DataProcessing.currentTime))
                    {
                        status = "now taking off";
                        if (airplane.Delta != 0)
                            status = "now taking off, " + (airplane.Delta).ToString() + " min later";
                    }
                    else if (airplane.applicationTime.AddMinutes(airplane.Delta) < DataProcessing.currentTime)
                    {
                        status = "departured on time";
                        if (airplane.Delta != 0)
                            status = "departured " + airplane.Delta.ToString() + " min later";
                    }
                    else
                    {
                        if (airplane.Delta == 0)
                            status = "should take off on time";
                        else
                            status = "departure is delayed for " + airplane.Delta.ToString() + " min";
                    }
                    string[] plane = { airplane.applicationTime.ToShortTimeString(), airplane.Flight, airplane.CompanyName, airplane.type, status};
                    dataGridViewDepartureSchedule.Rows.Add(plane);
                }
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

        private void openFileDialogAirplanesWithTheirTypes_FileOk(object sender, CancelEventArgs e)
        {
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

        private void tabPageData_Click(object sender, EventArgs e)
        {

        }

        private void FormRunwayController_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
           // this.Show();
        }

        private void buttonNextStep_Click(object sender, EventArgs e)
        {
            DataProcessing.currentTime = DataProcessing.currentTime.AddMinutes(DataProcessing.modulationStep);
            labelNowTime.Text = DataProcessing.currentTime.ToShortTimeString();
            fill_separeted_schedules();
            if ((int.Parse(comboBoxModulationStartTimeHours.Text) == DataProcessing.currentTime.Hour) && (int.Parse(comboBoxModulationStartTimeMinutes.Text) == DataProcessing.currentTime.Minute))
            {
                modulationEnded = true;
                buttonNextStep.Text = "modulation ended";
                changeVisibleButtons();
            }

        }

        private void labelNowHour_Click(object sender, EventArgs e)
        {

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void labelNowMinutes_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void labelCurrentTime_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
