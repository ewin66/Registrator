﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using Registrator.Module.BusinessObjects;
using DevExpress.ExpressApp.Scheduler.Win;
using DevExpress.XtraScheduler;
using System.Drawing;
using DevExpress.XtraScheduler.UI;
using DevExpress.XtraScheduler.Drawing;
using DevExpress.Utils;
using DevExpress.ExpressApp.Xpo;

namespace Registrator.Module.Win.Controllers
{
    public class DoctorEventWinController : ObjectViewController<ObjectView, Registrator.Module.BusinessObjects.DoctorEvent>
    {
        private const string FILTERKEY = "FilterDoctors";
        private Dictionary<Guid, DoctorEvent> eventsDict = new Dictionary<Guid, DoctorEvent>();

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();

            ListView listView = View as ListView;
            DetailView detailView = View as DetailView;
            
            if (listView != null)
            {
                IObjectSpace os = Application.CreateObjectSpace();
                listView.CollectionSource.CriteriaApplied += (o, e) =>
                {
                    // Предварительная загрузка расписаний докторов
                    var collectionSB = (CollectionSourceBase)o;
                    if (collectionSB.Criteria != null)
                    {
                        var events = new List<DoctorEvent>(os.GetObjects<DoctorEvent>(collectionSB.Criteria[FILTERKEY]));
                        eventsDict = events.ToDictionary(de => de.Oid, de => de);
                    }
                };

                SchedulerListEditor listEditor = ((ListView)View).Editor as SchedulerListEditor;
                if (listEditor != null)
                {
                    SchedulerControl scheduler = listEditor.SchedulerControl;
                    if (scheduler != null)
                    {
                        // Кастомизация надписи в расписании доктора
                        scheduler.InitAppointmentDisplayText += (o, e) =>
                        {
                            Guid guid = (Guid)e.Appointment.Id;
                            if (eventsDict.ContainsKey(guid))
                            {
                                var doctorEvent = eventsDict[guid];
                                e.Text = doctorEvent != null && doctorEvent.Pacient != null ? doctorEvent.Pacient.FullName : string.Empty;
                            }
                        };

                        #region Кастомизация Тултипа расписания доктора

                        // https://documentation.devexpress.com/WindowsForms/118551/Controls-and-Libraries/Scheduler/Visual-Elements/Scheduler-Control/Appointment-Flyout
                        scheduler.OptionsView.ToolTipVisibility = ToolTipVisibility.Always;
                        scheduler.OptionsCustomization.AllowDisplayAppointmentFlyout = false;
                        scheduler.ToolTipController = new ToolTipController();
                        scheduler.ToolTipController.ToolTipType = ToolTipType.SuperTip;
                        scheduler.ToolTipController.BeforeShow += (o, e) =>
                        {
                            var toolTipController = (ToolTipController)o;
                            AppointmentViewInfo aptViewInfo = null;
                            try
                            {
                                aptViewInfo = (AppointmentViewInfo)toolTipController.ActiveObject;
                            }
                            catch
                            {
                                return;
                            }

                            if (aptViewInfo == null) return;

                            Guid guid = (Guid)aptViewInfo.Appointment.Id;
                            if (!eventsDict.ContainsKey(guid))
                            {
                                // В словаре нет ключа т.к. расписание было только что создано
                                var events = new List<DoctorEvent>(ObjectSpace.GetObjects<DoctorEvent>(listView.CollectionSource.Criteria[FILTERKEY]));
                                eventsDict = events.ToDictionary(de => de.Oid, de => de);
                            }
                            DoctorEvent doctorEvent = eventsDict[guid];
                            // Вид талона (метка)
                            AppointmentLabel label = scheduler.Storage.Appointments.Labels.GetById(doctorEvent.Label);
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine(string.Format("Время: с {0:HH:mm} по {1:HH:mm}", doctorEvent.StartOn, doctorEvent.EndOn));
                            sb.AppendLine(string.Format("Пациент: {0}", doctorEvent.Pacient != null ? doctorEvent.Pacient.FullName : null));
                            sb.AppendLine(string.Format("Кем создано: {0}", doctorEvent.CreatedBy != null ? doctorEvent.CreatedBy.FullName : null));
                            sb.AppendLine(string.Format("Кто записал: {0}", doctorEvent.EditedBy != null ? doctorEvent.EditedBy.FullName : null));
                            sb.AppendLine(string.Format("Вид талона: {0}", label.DisplayName));
                            if (doctorEvent.Pacient != null)
                                sb.AppendLine(string.Format("Источник записи: {0}", CaptionHelper.GetDisplayText(doctorEvent.SourceType)));

                            SuperToolTip SuperTip = new SuperToolTip();
                            SuperToolTipSetupArgs args = new SuperToolTipSetupArgs();
                            args.Contents.Text = sb.ToString();
                            args.Contents.Font = new Font("Times New Roman", 11);
                            SuperTip.Setup(args);
                            e.SuperTip = SuperTip;
                        };

                        #endregion
                        
                        // Кастомизация коллекции меток
                        var storage = scheduler.Storage;
                        IAppointmentLabelStorage labelStorage = storage.Appointments.Labels;
                        FillLabelStorage(labelStorage);

                        #region Кастомизация всплывающего меню на расписании врача
                        DoctorEvent recordedEvent = null;
                        VisitCase visitCase = null;

                        // Всплывающее меню на расписании врача
                        scheduler.PopupMenuShowing += (o, e) =>
                        {
                            Pacient pacient = listView.Tag as Pacient;
                            AppointmentBaseCollection appoinments = (o as SchedulerControl).SelectedAppointments;
                            Appointment appoinment = appoinments.Count == 1 ? appoinments[0] : null;
                            if (appoinment == null) return;
                            
                            Guid key = (Guid)appoinment.Id;
                            DoctorEvent dEvent = appoinment != null && eventsDict.ContainsKey(key) ? eventsDict[key] : null;

                            if (e.Menu.Id != DevExpress.XtraScheduler.SchedulerMenuItemId.AppointmentMenu) return;
                            if (pacient != null && dEvent.Pacient == null & recordedEvent == null && dEvent.StartOn > DateTime.Now)
                            {
                                e.Menu.Items.Insert(0, new SchedulerMenuItem("Записать пациента", (o_, e_) =>
                                {
                                    IObjectSpace dEventObjectSpace = XPObjectSpace.FindObjectSpaceByObject(dEvent);
                                    dEvent.Pacient = dEventObjectSpace.GetObject(pacient);
                                    dEventObjectSpace.CommitChanges();
                                    // Обновление списочного представления
                                    listView.CollectionSource.Reload();
                                    // Расписание на которое записан пациент
                                    recordedEvent = dEvent;
                                    // Создание посещения для пациента
                                    visitCase = VisitCase.CreateVisitCase(dEventObjectSpace,
                                        dEventObjectSpace.GetObject(pacient),
                                        dEvent.AssignedTo, dEvent.StartOn);
                                    dEventObjectSpace.CommitChanges();
                                }));
                            }
                            else if (dEvent != null && dEvent.Pacient != null)
                            {
                                e.Menu.Items.Insert(0, new SchedulerMenuItem("Отменить запись", (o_, e_) =>
                                {
                                    IObjectSpace dEventObjectSpace = XPObjectSpace.FindObjectSpaceByObject(dEvent);
                                    dEvent.Pacient = null;
                                    dEventObjectSpace.CommitChanges();
                                    // Обновление списочного представления
                                    listView.CollectionSource.Reload();
                                    // Отмена записи пациента на выбранное расписание
                                    if (recordedEvent != null && recordedEvent.Oid == dEvent.Oid)
                                        recordedEvent = null;
                                    if (visitCase != null)
                                    {
                                        dEventObjectSpace.Delete(visitCase);
                                        dEventObjectSpace.CommitChanges();
                                    }
                                }));
                            }
                        };

                        #endregion

                        // Кастомизация цвета фона расписания:
                        // Если выбранный пациент уже записан, то цвет расписания окрашивается в светло-розовый.
                        // Если в расписании записан другой пациент, то цвет расписания окрашивается в морковный.
                        scheduler.AppointmentViewInfoCustomizing += (o_, e_)=>
                        {
                            Pacient pacient = listView.Tag as Pacient;
                            if (pacient != null)
                            {
                                Guid guid = (Guid)e_.ViewInfo.Appointment.Id;
                                if (eventsDict.ContainsKey(guid) && eventsDict[guid].Pacient != null)
                                {
                                    e_.ViewInfo.Appearance.BackColor = eventsDict[guid].Pacient.Oid == pacient.Oid ?
                                        Color.FromArgb(255, 192, 192) : Color.FromArgb(255, 128, 0);
                                }
                            }
                        };
                    }
                }
            }
            else if (detailView != null)
            {
                // Кастомизация коллекции меток
                foreach (SchedulerLabelPropertyEditor pe in ((DetailView)View).GetItems<SchedulerLabelPropertyEditor>())
                    if (pe.Control != null)
                    {
                        ISchedulerStorage storage = ((AppointmentLabelEdit)pe.Control).Storage;
                        IAppointmentLabelStorage labelStorage = storage.Appointments.Labels;
                        FillLabelStorage(labelStorage);
                    }
            }
        }

        private void FillLabelStorage(IAppointmentLabelStorage labelStorage)
        {
            labelStorage.Clear();
            int i = 0;
            using (IObjectSpace os = Application.CreateObjectSpace())
            {
                IAppointmentLabel label = labelStorage.CreateNewLabel(i, "Нет", "Нет");
                label.SetColor(Color.White);
                labelStorage.Add(label);
                i++;

                IList<DoctorEventLabel> labels = os.GetObjects<DoctorEventLabel>();
                foreach (var doctorEventLabel in labels)
                {
                    label = labelStorage.CreateNewLabel(i, doctorEventLabel.Name, doctorEventLabel.Name);
                    label.SetColor(doctorEventLabel.Color);
                    labelStorage.Add(label);
                    i++;
                }
            }
        }
    }
}
