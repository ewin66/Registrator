﻿using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.StateMachine;
using DevExpress.ExpressApp.Xpo;
using Registrator.Module.BusinessObjects.Dictionaries;
using DevExpress.ExpressApp.DC;
using System.Xml.Linq;
using DevExpress.Xpo;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Base;
using Registrator.Module.BusinessObjects.Interfaces;
using System.ComponentModel;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Editors;
using Registrator.Module.BusinessObjects.StateMachines;

namespace Registrator.Module.BusinessObjects.Abstract
{
    /// <summary>
    /// Представляет абстрактный случай с мин. полями
    /// </summary>
    [DefaultClassOptions]
    [XafDisplayName("Случаи")]
    public abstract class AbstractCase : BaseObject, IWorkflowCaseStateProvider
    {
        public AbstractCase(Session session) : base (session)
        { }

        public override void AfterConstruction()
        {
            base.AfterConstruction();

            this.DateIn = DateTime.Now;

            string MOCode = Settings.MOSettings.GetCurrentMOCode(Session);
            this.LPU = Session.FindObject<MedOrg>(CriteriaOperator.Parse("Code=?", MOCode));
            this.LPU_1 = MOCode;

            // пока используем ГУИД идентификатор объекта
            this.NHistory = this.Oid.ToString();
        }

        #region Мин. поля для реестра ТФОМС
        /// <summary>
        /// Обязательное поле N(11)
        /// Номер записи в реестре случаев
        /// Соответствует порядковому номеру записи реестра счёта на бумажном носителе при его предоставлении.
        /// </summary>
        [XafDisplayName("Идентификатор случая для пациента (порядковый номер)")]
        [Browsable(false)]
        public int Id {get;set;}

		/// <summary>
        /// Обяательное поле N(4)
		/// Вид медицинской помощи
        /// Классификатор видов медицинской помощи. Справочник V008 (VidMedPom) Приложения А.
		/// </summary>
        [XafDisplayName("Вид мед. помощи")]
        [Browsable(false)]
        public VidMedPomoshi VidPom { get; set; }
				
        /// <summary>
        /// Обязательное поле T(6)
        /// МО лечения, указывается в соответствии с реестром F003.
        /// </summary>
        [XafDisplayName("Лечащее МО")]
        [Browsable(false)]
        public MedOrg LPU { get; set; }

        /// <summary>
        /// Условно-обязательное поле T(8)
        /// Подразделение МО лечения из регионального справочника.
        /// </summary>
        [XafDisplayName("Код подразделения МО")]
        [Browsable(false)]
        public string LPU_1 {get; set;}

        /// <summary>
        /// Обязательное поле T(50)
        /// Номер истории болезни/ талона амбулаторного пациента/ карты вызова скорой медицинской помощи
        /// </summary>
        [Browsable(false)]
        public string NHistory {get;set;}

        /// <summary>
        /// Обязательное поле D
        /// Дата начала лечения	
        /// </summary>
        [XafDisplayName("Дата начала лечения")]
        public DateTime DateIn { get; set; }

        /// <summary>
        /// Обязательное поле D
        /// Дата окончания лечения	
        /// </summary>
        [XafDisplayName("Дата окончания лечения")]
        public DateTime DateOut {get;set;}
        
        /// <summary>
        /// Диагноз основной T(10)
        /// </summary>
        [XafDisplayName("Основной диагноз")]
        [DataSourceCriteriaProperty("DiagnoseCriteria")]
        [DevExpress.Xpo.Aggregated]
        public MKBWithType MainDiagnose { get; set; }

        [NonPersistent]
        [Browsable(false)]
        public abstract CriteriaOperator DiagnoseCriteria { get; set; }

        /// <summary>
        /// Обязательное поле N(2)
        /// Способо оплаты мед. помощи
        /// </summary>
        [XafDisplayName("Способ оплаты мед. помощи")]
        [Browsable(false)]
        public SposobOplatiMedPom SposobOplMedPom { get; set; }

        /// <summary>
        /// Условно-обязательное поле N(5.2)
        /// Количество единиц оплаты медицинской помощи	
        /// </summary>
        [XafDisplayName("Кол-во")]
        [Browsable(false)]
        public Decimal MedPomCount { get; set; }

        /// <summary>
        /// Условно-обязательное поле N(15.2)
        /// Тариф
        /// </summary>
        [XafDisplayName("Тариф")]
        [Browsable(false)]
        public Decimal Tarif { get; set; }

        /// <summary>
        /// Обязательное поле N(15.2)
        /// Сумма, выставленная к оплате
        /// </summary>
        [XafDisplayName("Общая сумма")]
        [Browsable(false)]
        public virtual Decimal TotalSum { get; set; }

        /// <summary>
        /// Обязательное поле N(1)
        /// Оплата случая оказания мед. помощи
        /// </summary>
        [XafDisplayName("Статус оплаты")]
        [Browsable(false)]
        public Oplata StatusOplati { get; set; }

        /*
        SUMP	У	N(15.2)	Сумма, принятая к оплате СМО (ТФОМС)	Заполняется СМО (ТФОМС).
        SANK_IT	У	N(15.2)	Сумма санкций по случаю	Равна сумме описанных ниже санкций.
        SANK	УМ	S	Сведения о санкциях	
        */

        /// <summary>
        /// Условно-обязательное поле T(250)
        /// Служебная информация
        /// </summary>
        [XafDisplayName("Служебная информация")]
        [Browsable(false)]
        public string Comment { get; set; }
        #endregion

        [Association("AbstractCase-Pacient")]
        [ImmediatePostData]
        public Pacient Pacient { get; set; }

        // Разница времени создания случая и даты рождения пациента
        [NonPersistent]
        [Browsable(false)]
        public TimeSpan CaseCreatedPacientAge
        {
            get
            {
                if (Pacient.Birthdate != null)
                    return this.DateIn - Pacient.Birthdate.Value;
                return new TimeSpan();
            }
        }

        
        public IList<IStateMachine> GetStateMachines()
        {
            List<IStateMachine> result = new List<IStateMachine>();
            result.Add(new WorkflowCaseStatusStateMachine(XPObjectSpace.FindObjectSpaceByObject(this)));
            return result;
        }
        [Browsable(false)]
        public WorkflowCaseStatus WorkflowCaseStatus { get; set; }
    }

    /// <summary>
    /// Общие случаи: госпитализация, посещение и т.п.
    /// </summary>
    [Persistent]
    public abstract class CommonCase : AbstractCase, IReestrTFoms
    {
        private VidUsloviyOkazMedPomoshi _uslovia;

        public CommonCase(Session session)
            : base(session)
        { }

        public override void AfterConstruction()
        {
            base.AfterConstruction();
            this.PreDiagnose = new MKBWithType(Session);
            this.MainDiagnose = new MKBWithType(Session);

            // для общих случаев по умолчанию всегда добавляется как минимум одна основная услуга
            Services.Add(new MedService(Session) {IsMainService = true, AutoOpen = false});

            // определяем текущего пользователя
            var createdBy = SecuritySystem.CurrentUser as Doctor;
            if (createdBy != null)
            {
                // находим доктора с таким же Логином
                var doctor = Session.FindObject<Doctor>(CriteriaOperator.Parse("UserName=?", createdBy.UserName));

                if (doctor != null)
                {
                    this.Doctor = doctor;
                }
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (this.MainDiagnose == null)
            {
                this.MainDiagnose = new MKBWithType(Session);
            }

            if (this.PreDiagnose == null)
            {
                this.PreDiagnose = new MKBWithType(Session);
            }

            this.Services.Load();
        }

        #region Мин. поля для реестра ТФОМС

        /// <summary>
        /// Обязательное поле N(1)
        /// Форма оказания медицинской помощи
        /// Классификатор форм оказания медицинской помощи. Справочник V014 Приложения А
        /// </summary>
        [XafDisplayName("Форма мед. помощи")]
        public FormaMedPomoshi FormaPomoshi { get; set; }

        /// <summary>
        /// Условно-обязательное поле T(6)
        /// Код МО, направившего на лечение (диагностику, консультацию)
        /// </summary>
        [XafDisplayName("Направившее ЛПУ")]
        [DataSourceCriteriaProperty("LPUCriteria")]
        [LookupEditorMode(LookupEditorMode.AllItemsWithSearch)]
        public MedOrg FromLPU { get; set; }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [NonPersistent]
        [Browsable(false)]
        private CriteriaOperator LPUCriteria
        {
            get
            {
                var okato = Settings.RegionSettings.GetCurrentRegionOKATO(Session);
                return CriteriaOperator.Parse("TF_OKATO=?", okato);
            }
        }

        /// <summary>
        /// Условно-обязательное поле N(8)
        /// Код отделения
        /// Отделение МО лечения из регионального справочника
        /// </summary>
        [XafDisplayName("Отделение МО")]
        [Browsable(false)]
        public Otdel Otdelenie { get; set; }

        /// <summary>
        /// Обязательное поле N(3)
        /// Код профиля
        /// Классификатор профилей оказанной медицинской помощи (V002)
        /// </summary>
        [XafDisplayName("Мед. профиль")]
        [Browsable(false)]
        public MedProfil Profil { get; set; }

        /// <summary>
        /// Обязательное поле N(1)
        /// Признак детского профиля (0-нет, 1-да)
        /// Заполняется в зависимости от профиля оказанной медицинской помощи
        /// </summary>

        [Browsable(false)]
        public PriznakDetProfila DetProfil { get; set; }

        /// <summary>
        /// Необязательное поле
        /// Диагноз первичный
        /// </summary>
        [XafDisplayName("Первичный диагноз")]
        [Browsable(false)]
        [DevExpress.Xpo.Aggregated]
        public MKBWithType PreDiagnose { get; set; }

        /// <summary>
        /// Условно-обязательный множ.
        /// Диагноз сопутствующего заболевания T(10)
        /// </summary>
        [XafDisplayName("Диагнозы сопутствующих заболеваний")]
        public IList<MKB10> SoputsDiagnoses// {get;set;}
        {
            get
            {
                if (Services == null)
                    return null;

                //выбрать из диагнозов сопутствующие диагнозы
                var list = new List<MKB10>();

                // выбираем диагнозы

                return list;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Условно-обязательный множ.
        /// Диагноз осложнения заболеваия T(10)
        /// </summary>
        [XafDisplayName("Диагнозы осложнения заболевания")]
        public IList<MKB10> OslozhDiagnoses// {get;set;} 
        {
            get
            {
                if (Services == null)
                    return null;

                //выбрать из диагнозов диагнозы осложнения
                var list = new List<MKB10>();
                
                // выбираем диагнозы

                return list;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Диагнозы всех услуг
        /// </summary>

        public IList<MKBWithType> Diagnoses
        {
            get
            {
                var list = new List<MKBWithType>();
                foreach (var commonService in Services)
                {
                    list.AddRange(commonService.Diagnoses.ToList());
                }

                return list;
            }
        }
          
        /// <summary>
        /// Условно-обязательный множ. N(4)
        /// Вес при рождении
        /// Указывается при оказании медицинской помощи недоношенным и маловесным детям. Поле заполняется, если в качестве пациента указана мать
        /// </summary>
        [XafDisplayName("Вес при рождении")]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [Appearance("VesPriRozhd_Invisible", Context = "DetailView", Visibility = ViewItemVisibility.Hide, Criteria = "Iif(IsNull(Pacient), true, !Pacient.IsNewBorn)")]
        public int VesPriRozhdenii { get; set; }

        /*
        CODE_MES1	УМ	Т(20)	Код МЭС	Классификатор МЭС. Указывается при наличии утверждённого стандарта.
        CODE_MES2	У	Т(20)	Код МЭС сопутствующего заболевания	
        */

        /// <summary>
        /// Обязательное поле N(2)
        /// Условия оказания медицинской помощи
        /// Классификатор условий оказания медицинской помощи (V006 Приложения А)
        /// </summary>
        [XafDisplayName("Условия оказания мед. помощи")]
        [Browsable(false)]
        public VidUsloviyOkazMedPomoshi UsloviyaPomoshi 
        { 
            get {return _uslovia; }
            set
            {
                SetPropertyValue("UsloviyaPomoshi", ref _uslovia, value);
                OnChanged("Resultat");
                OnChanged("Ishod");
            }
        }

        /// <summary>
        /// Обязательное поле N(3)
        /// Результат обращения/госпитализации
        /// Классификатор результатов обращения за медицинской помощью (V009).
        /// </summary>
        [XafDisplayName("Результат лечения")]
        [DataSourceCriteriaProperty("ResultatCriteria")]
        [LookupEditorMode(LookupEditorMode.AllItemsWithSearch)]
        public ResultatObrasheniya Resultat { get; set; }
        
        /// <summary>
        /// Обязательное поле N(3)
        /// Исход заболевания
        /// Классификатор исходов заболевания (V012).
        /// </summary>
        [XafDisplayName("Исход заболевания")]
        [DataSourceCriteriaProperty("IshodCriteria")]
        [LookupEditorMode(LookupEditorMode.AllItemsWithSearch)]
        public IshodZabolevaniya Ishod { get; set; }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [NonPersistent]
        [Browsable(false)]
        public virtual CriteriaOperator ResultatCriteria
        {
            get
            {
                if (UsloviyaPomoshi == null)
                {
                    return CriteriaOperator.Parse("1!=1");
                }
                return CriteriaOperator.Parse("DlUslov=?", UsloviyaPomoshi.Code.ToString());
            }
        }

        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [NonPersistent]
        [Browsable(false)]
        private CriteriaOperator IshodCriteria
        {
            get
            {
                if (UsloviyaPomoshi == null)
                {
                    return CriteriaOperator.Parse("1!=1");
                }
                return CriteriaOperator.Parse("DL_USLOV=?", UsloviyaPomoshi.Code.ToString());
            }
        }

        /// <summary>
        /// Обязательное поле N(4)
        /// Специальность лечащего врача/ врача, закрывшего талон
        /// Классификатор медицинских специальностей (V015).Указывается значение параметра «Code»
        /// </summary>
        [XafDisplayName("Специальность лечащего врача")]
        [Browsable(false)]
        public DoctorSpecTree DoctorSpec { get; set; }

        /// <summary>
        /// Условно-обязательное поле T(4)
        /// Указывается код используемого справочника медицинских специальностей. Отсутствие поля обозначает использование справочника V004
        /// </summary>
        [Browsable(false)]
        public string VersionSpecClassifier { get; set; }

        /// <summary>
        /// Обязательное поле T(25)
        /// Код врача, закрывшего талон/историю болезни
        /// Территориальный справочник
        /// </summary>
        [XafDisplayName("Лечащий врач")]
        public Doctor Doctor { get; set; }

        /// <summary>
        /// Необязательное множ. поле N(1)
        /// Признак "Особый случай" при регистрации обращения за медицинской помощью
        /// Указываются все имевшиеся особые случаи.
        /// </summary>
        [XafDisplayName("Особый случай")]
        [Appearance("OsobiySluchay_Invisible", Context = "DetailView", Visibility = ViewItemVisibility.Hide, Criteria = "Iif(IsNull(Pacient), true, !Pacient.IsNewBorn)")]
        public PriznakOsobogoSluchaya OsobiySluchay { get; set; }
        #endregion

        [Association("CommonCase-CommonServices"), DevExpress.Xpo.Aggregated]
        public XPCollection<CommonService> Services 
        {
            get
            {
                var col = GetCollection<CommonService>("Services");
                col.CollectionChanged += col_CollectionChanged;
                return col;
            }
        }

        void col_CollectionChanged(object sender, XPCollectionChangedEventArgs e)
        {
            Session.Save(this);
        }

        public override decimal TotalSum
        {
            get
            {
                if (Services == null)
                    return 0;
                decimal sum = 0;
                foreach (var service in Services)
                    sum += service.Sum;

                return sum;
            }
            set
            {
                base.TotalSum = value;
            }
        }

        // интерфейс элемента реестра реализуется у потомков (у госпитализации свои необходимые поля)
        public abstract bool IsValidForReestr();
        public abstract XElement GetReestrElement();
        public abstract XElement GetReestrElement(int zapNumber);
    }

    /// <summary>
    /// Высокотехнологичные случаи
    /// Представляет общий случай с дополнительными полями
    /// Не используется
    /// </summary>
    abstract class HightechCase : CommonCase
    {
        public HightechCase(Session session)
            : base(session)
        { }

        #region Мин. поля для реестра ТФОМС
        /// <summary>
        /// Обязательное поле Т(9)
        /// Вид высокотехнологичной медицинской помощи
        /// Классификатор видов высокотехнологичной медицинской помощи. Справочник V018 Приложения А
        /// </summary>
        public string VidHighMedPom {get;set;}
        
        /// <summary>
        /// Обязательное поле N(3)
        /// Метод высокотехнологичной медицинской помощи
        /// Классификатор методов высокотехнологичной медицинской помощи. Справочник V019 Приложения А
        /// </summary>
        public string MethodHighMedPom {get;set;}
        #endregion

    }

    /// <summary>
    /// Случай диспансеризации
    /// </summary>
    [Persistent]
    public abstract class DispCase : AbstractCase, IReestrTFoms
    {
        public DispCase(Session session)
            : base(session)
        { }

        #region Мин. поля для реестра ТФОМС
        /// <summary>
        /// Обязательное поле N(1)
        /// Результат диспансеризации
        /// Классификатор результатов диспансеризации V017
        /// </summary>
        [XafDisplayName("Результат диспансеризации")]
        public ResultatDispanserizacii ResultD { get; set; }
        #endregion

        [Association("DispCase-DispServices"), DevExpress.Xpo.Aggregated]
        public XPCollection<DispService> Services
        {
            get { return GetCollection<DispService>("Services"); }}


        // методы интерфейса реализует конкретный случай
        public abstract bool IsValidForReestr();

        public abstract XElement GetReestrElement();

        public abstract XElement GetReestrElement(int zapNumber);
    }
}
