﻿using System;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using System.Xml.Linq;
namespace Registrator.Module.BusinessObjects.Dictionaries
{
    /// <summary>
    /// Классификатор изготовления полиса
    /// </summary>
    public class R002 : BaseObject
    {
        // пример записи в XML:
        // <rec1 Kod="0" Opis="Не требует изготовления полиса" DATEBEG="01.01.2011" DATEEND="" />
        // <rec2 Kod="1" Opis="Бумажный полис ОМС" DATEBEG="01.01.2011" DATEEND="" />

        public R002(Session session) : base(session) { }

        /// <summary>
        /// Код
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Наименование
        /// </summary>
        [Size(255)]
        public string Name { get; set; }

        /// <summary>
        /// Дата начала действия
        /// </summary>
        public DateTime? DateBeg { get; set; }

        /// <summary>
        /// Дата окончания действия
        /// </summary>
        public DateTime? DateEnd { get; set; }

        /// <summary>
        /// Добавляет в базу классификаторы из файла XML
        /// </summary>
        /// <param name="updater">Пространство объектов ObjectSpace</param>
        /// <param name="xmlPath">Путь до файла классификатора</param>
        public static void UpdateDbFromXml(DevExpress.ExpressApp.IObjectSpace objSpace, string xmlPath)
        {
            XDocument doc = XDocument.Load(xmlPath);

            const string elementNameStartsWith = "rec";

            const string code_attr = "Kod";
            const string name_attr = "Opis";
            const string dateBeg_attr = "DATEBEG";
            const string dateEnd_attr = "DATEEND";

            foreach (var element in doc.Root.Elements())
            {
                if (element.Name.ToString().StartsWith(elementNameStartsWith) == false) continue;

                R002 obj = objSpace.FindObject<R002>(DevExpress.Data.Filtering.CriteriaOperator.Parse("Code=?", element.Attribute(code_attr).Value));
                if (obj == null)
                {
                    obj = objSpace.CreateObject<R002>();
                    obj.Code = int.Parse(element.Attribute(code_attr).Value);
                    obj.Name = element.Attribute(name_attr).Value;
                    obj.DateBeg = element.Attribute(dateBeg_attr).Value == "" ? null : (DateTime?)Convert.ToDateTime(element.Attribute(dateBeg_attr).Value);
                    obj.DateEnd = element.Attribute(dateEnd_attr).Value == "" ? null : (DateTime?)Convert.ToDateTime(element.Attribute(dateEnd_attr).Value);
                }
            }
        }
    }
}