
using System;
using System.Collections.Generic;
using Contensive.BaseClasses;

namespace aoEmailBounceProcess {
    namespace Models.Domain {
        public class BlankDomainModel : BaseDomainModel {
            // 
            // ====================================================================================================
            // -- instance properties
            //
            // -- sample model properties. Each property name matches a database field, and the type matches the Contensive use case.
            // -- These properties will save the default value for the type if not initialized.
            //
            public string sampleDbFieldText { get; set; }
            public int sampleDbFieldInteger { get; set; }
            public bool sampleDbFieldBoolean { get; set; }
            public DateTime sampleDbFieldDate { get; set; }
            public double sampleDbFieldNumber { get; set; }
            //
            // -- sample model nullable properties. These properties will save null if not initialized.
            //
            public int? sampleDbFieldNullableInteger { get; set; }
            public bool? sampleDbFieldNullableBoolean { get; set; }
            public DateTime? sampleDbFieldNullableDate { get; set; }
            public double? sampleDbFieldNullableNumber { get; set; }
            // 
            // ====================================================================================================
            /// <summary>
            /// create list of records for this domain model
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="organizationId"></param>
            /// <param name="pageSize"></param>
            /// <param name="pageNumber"></param>
            /// <returns></returns>
            public static List<BlankDomainModel> createList(CPBaseClass cp, int organizationId, int pageSize, int pageNumber) {
                string sql = Contensive.Addons.EmailBounceProcess.Properties.Resources.sampleSql.Replace("{organizationId}", organizationId.ToString());
                return createListFromSql<BlankDomainModel>(cp, sql, pageSize, pageNumber);
            }
            // 
            // ====================================================================================================
            /// <summary>
            /// create list of records for this domain model
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="organizationId"></param>
            /// <param name="pageSize"></param>
            /// <returns></returns>
            public static List<BlankDomainModel> createList(CPBaseClass cp, int organizationId, int pageSize)
                => createList(cp, organizationId, pageSize, 1);
            // 
            // ====================================================================================================
            /// <summary>
            /// create list of records for this domain model
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="organizationId"></param>
            /// <returns></returns>
            public static List<BlankDomainModel> createList(CPBaseClass cp, int organizationId)
                => createList(cp, organizationId, 99999, 1);
        }
    }
}
