﻿using System;
using System.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Globalization;
using System.Web.Script.Serialization;

namespace Geckoboard
{
    public class DatasetsClient
    {
        public Connection connection;

        public DatasetsClient(Connection connection)
        {
            this.connection = connection;
        }

        public Dataset FindOrCreate(string datasetId, IEnumerable<Field> fields)
        {
            return FindOrCreate(datasetId, fields, new string[0]);
        }

        public Dataset FindOrCreate(string datasetId, IEnumerable<Field> fields, string[] uniqueBy)
        {
            string path = "/datasets/" + Uri.EscapeDataString(datasetId);
            JsonObject json = new JsonObject();
            JsonObject jsonFields = new JsonObject();

            foreach (var field in fields)
            {
                jsonFields[field.Id] = field.ToJson();
            }

            json["fields"] = jsonFields;

            if (uniqueBy.Length > 0)
            {
                json["unique_by"] = new JavaScriptSerializer().Serialize(uniqueBy);
            }

            var response = connection.Put(path, json.ToString());

            return new Dataset(this, (JsonObject)JsonValue.Parse(response.Content.ReadAsStringAsync().Result));           
        }

        public bool Delete(string datasetId)
        {
            string path = "/datasets/" + Uri.EscapeDataString(datasetId);
            connection.Delete(path);

            return true;
        }

        public bool PutData(Dataset dataset, IEnumerable<IDictionary<string, object>> data)
        {
            string path = "/datasets/" + Uri.EscapeDataString(dataset.Id) + "/data";

            connection.Put(path, FormatData(dataset, data));

            return true;
        }

        public bool PostData(Dataset dataset, IEnumerable<IDictionary<string, object>> data)
        {
            return PostData(dataset, data, null);
        }

        public bool PostData(Dataset dataset, IEnumerable<IDictionary<string, object>> data, string deleteBy)
        {
            string path = "/datasets/" + Uri.EscapeUriString(dataset.Id) + "/data";

            connection.Post(path, FormatData(dataset, data, deleteBy));

            return true;
        }

        public string FormatData(Dataset dataset, IEnumerable<IDictionary<string, object>> data)
        {
            return FormatData(dataset, data, null);
        }

        public string FormatData(Dataset dataset, IEnumerable<IDictionary<string, object>> data, string deleteBy)
        {
            JsonArray jsonArray = new JsonArray();

            foreach (var dataPoint in data)
            {
                JsonObject json = new JsonObject();

                foreach (var key in dataPoint.Keys)
                {
                    switch (dataset.Fields[key].Type)
                    {
                        case "date":
                            DateTime date = (DateTime)dataPoint[key];
                            json[dataset.Fields[key].Id] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                            break;
                        case "datetime":
                            DateTime datetime = (DateTime)dataPoint[key];
                            json[dataset.Fields[key].Id] = datetime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
                            break;
                        case "number":
                        case "money":
                            json[dataset.Fields[key].Id] = (int)dataPoint[key];
                            break;
						case "string":
                            json[dataset.Fields[key].Id] = (string)dataPoint[key];
							break;
						case "percentage":
                            json[dataset.Fields[key].Id] = (double)dataPoint[key];
							break;
                    }
                }

                jsonArray.Add(json);
            }

            var wrapper = new JsonObject();
            wrapper["data"] = jsonArray;

            if (!String.IsNullOrEmpty(deleteBy))
            {
                wrapper["delete_by"] = deleteBy;
            }

            return wrapper.ToString();
        }
    }
}
