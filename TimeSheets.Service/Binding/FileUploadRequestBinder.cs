using Cmas.Services.TimeSheets.Dtos.Requests;
using Nancy;
using Nancy.ModelBinding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cmas.Services.TimeSheets.Binding
{
    public class FileUploadRequestBinder : IModelBinder
    {
        public object Bind(NancyContext context, Type modelType, object instance, BindingConfig configuration, params string[] blackList)
        {
            var fileUploadRequest = (instance as FileUploadRequest) ?? new FileUploadRequest();

            var form = context.Request.Form;
            
            fileUploadRequest.Title = form["title"];
            fileUploadRequest.Description = form["description"];
            fileUploadRequest.File = GetFileByKey(context, "file");
            
            return fileUploadRequest;
        }
        
        private HttpFile GetFileByKey(NancyContext context, string key)
        {
            IEnumerable<HttpFile> files = context.Request.Files;
            if (files != null)
            {
                return files.FirstOrDefault(x => x.Key.Equals(key, StringComparison.CurrentCultureIgnoreCase));
            }
            return null;
        }

        public bool CanBind(Type modelType)
        {
            return modelType == typeof(FileUploadRequest);
        }
    }
}
