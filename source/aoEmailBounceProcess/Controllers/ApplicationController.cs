
using System;
using System.Collections.Generic;
using Contensive.BaseClasses;
using static aoEmailBounceProcess.Constants;

namespace aoEmailBounceProcess {
    namespace Controllers {
        // 
        // ====================================================================================================
        /// <summary>
        ///     ''' 
        ///     ''' </summary>
        ///     ''' <remarks></remarks>
        public class ApplicationController : IDisposable {
            // 
            private readonly CPBaseClass cp;
            // 
            // ====================================================================================================
            /// <summary>
            /// Errors accumulated during rendering.
            /// </summary>
            public List<ResponseErrorClass> responseErrorList { get; set; } = new List<ResponseErrorClass>();
            // 
            // ====================================================================================================
            /// <summary>
            /// data accumulated during rendering
            /// </summary>
            public List<ResponseNodeClass> responseNodeList { get; set; } = new List<ResponseNodeClass>();
            // 
            // ====================================================================================================
            /// <summary>
            /// list of name/time used to performance analysis
            /// </summary>
            public List<ResponseProfileClass> responseProfileList { get; set; } = new List<ResponseProfileClass>();
            // 
            // ====================================================================================================
            /// <summary>
            /// Constructor, authentication can be disabled
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="requiresAuthentication"></param>
            public ApplicationController(CPBaseClass cp, bool requiresAuthentication) {
                this.cp = cp;
                if ((requiresAuthentication & !cp.User.IsAuthenticated)) {
                    throw new UnauthorizedAccessException();
                }
            }
            // 
            // ====================================================================================================
            /// <summary>
            /// constructor, authentication required
            /// </summary>
            /// <param name="cp"></param>
            public ApplicationController(CPBaseClass cp) {
                this.cp = cp;
            }
            // 
            // ====================================================================================================
            /// <summary>
            /// get the serialized results
            /// </summary>
            /// <returns></returns>
            public string getResponse() {
                try {
                    return cp.JSON.Serialize(new ResponseClass() {
                        success = responseErrorList.Count.Equals(0),
                        nodeList = responseNodeList,
                        errorList = responseErrorList,
                        profileList = responseProfileList
                    });
                } catch (Exception ex) {
                    cp.Site.ErrorReport(ex);
                    throw;
                }
            }
            // 
            // ====================================================================================================
            /// <summary>
            /// The user is not authenticated and this activity is not for anonymous access
            /// </summary>
            /// <param name="cp"></param>
            /// <returns></returns>
            public static string getResponseUnauthorized(CPBaseClass cp) {
                cp.Response.SetStatus(HttpErrorEnum.unauthorized + " Unauthorized");
                return string.Empty;
            }
            // 
            // ====================================================================================================
            /// <summary>
            /// The user is authenticated, but their role does not allow this activity
            /// </summary>
            /// <param name="cp"></param>
            /// <returns></returns>
            public static string getResponseForbidden(CPBaseClass cp) {
                cp.Response.SetStatus(HttpErrorEnum.forbidden + " Forbidden");
                return string.Empty;
            }
            // 
            // ====================================================================================================
            //
            public static string getResponseServerError(CPBaseClass cp) {
                cp.Response.SetStatus(HttpErrorEnum.internalServerError + " Internal Server Error");
                return string.Empty;
            }
            // 
            // ==========================================================================================
            // -- Disposable support
            //
            protected bool disposed = false;
            /// <summary>
            /// dispose
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing) {
                if (!this.disposed) {
                    if (disposing) {
                        //
                        // -- dispose non-managed resources
                    }
                }
                this.disposed = true;
            }
            // Do not change or add Overridable to these methods.
            // Put cleanup code in Dispose(ByVal disposing As Boolean).
            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            ~ApplicationController() {
                Dispose(false);
                //base.Finalize();
            }
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// list of events and their stopwatch times
        /// </summary>
        [Serializable()]
        public class ResponseProfileClass {
            public string name;
            public long time;
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// remote method top level data structure
        /// </summary>
        [Serializable()]
        public class ResponseClass {
            public bool success = false;
            public List<ResponseErrorClass> errorList = new List<ResponseErrorClass>();
            public List<ResponseNodeClass> nodeList = new List<ResponseNodeClass>();
            public List<ResponseProfileClass> profileList;
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// data store for jsonPackage
        /// </summary>
        [Serializable()]
        public class ResponseNodeClass {
            public string dataFor = "";
            public object data; // IEnumerable(Of Object)
        }
        // 
        // ====================================================================================================
        /// <summary>
        ///         ''' error list for jsonPackage
        ///         ''' </summary>
        [Serializable()]
        public class ResponseErrorClass {
            public int number = 0;
            public string description = "";
        }
    }
}
