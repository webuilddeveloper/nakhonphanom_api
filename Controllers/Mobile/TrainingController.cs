using System;
using System.Collections.Generic;
using System.Linq;
using cms_api.Extension;
using cms_api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace mobile_api.Controllers
{
    [Route("m/[controller]")]
    public class TrainingController : Controller
    {
        public TrainingController() { }

        // POST /read
        [HttpPost("read")]
        public ActionResult<Response> Read([FromBody] Criteria value)
        {
            try
            {
                var col = new Database().MongoClient<EventCalendar>( "training");
                var filter = (Builders<EventCalendar>.Filter.Eq("status", "A") & value.filterOrganization<EventCalendar>());

                if (!string.IsNullOrEmpty(value.code)) { filter = filter & Builders<EventCalendar>.Filter.Eq("code", value.code); }
                if (!string.IsNullOrEmpty(value.keySearch)) { filter = filter & Builders<EventCalendar>.Filter.Regex("title", new BsonRegularExpression(string.Format(".*{0}.*", value.keySearch), "i")); }
                if (!string.IsNullOrEmpty(value.category)) { filter = filter & Builders<EventCalendar>.Filter.Eq("category", value.category); }
                if (!string.IsNullOrEmpty(value.status)) { filter = filter & Builders<EventCalendar>.Filter.Eq("status", value.status); }
                if (!string.IsNullOrEmpty(value.description)) { filter = filter & Builders<EventCalendar>.Filter.Regex("description", new BsonRegularExpression(string.Format(".*{0}.*", value.description), "i")); }
                if (!string.IsNullOrEmpty(value.language)) { filter = filter & Builders<EventCalendar>.Filter.Regex("language", value.language); }
                if (value.isHighlight) { filter = filter & Builders<EventCalendar>.Filter.Eq("isHighlight", value.isHighlight); }

                var ds = value.startDate.toDateFromString().toBetweenDate();
                var de = value.endDate.toDateFromString().toBetweenDate();
                if (value.startDate != "Invalid date" && value.endDate != "Invalid date" && !string.IsNullOrEmpty(value.startDate) && !string.IsNullOrEmpty(value.endDate)) { filter = filter & Builders<EventCalendar>.Filter.Gt("docDate", ds.start) & Builders<EventCalendar>.Filter.Lt("docDate", de.end); }
                else if (value.startDate != "Invalid date" && !string.IsNullOrEmpty(value.startDate)) { filter = filter & Builders<EventCalendar>.Filter.Gt("docDate", ds.start) & Builders<EventCalendar>.Filter.Lt("docDate", ds.end); }
                else if (value.endDate != "Invalid date" && !string.IsNullOrEmpty(value.endDate)) { filter = filter & Builders<EventCalendar>.Filter.Gt("docDate", de.start) & Builders<EventCalendar>.Filter.Lt("docDate", de.end); }
                //filter = filter & (Builders<BsonDocument>.Filter.Eq(x => x.B, "4") | Builders<User>.Filter.Eq(x => x.B, "5"));

                var docs = col.Find(filter).SortBy(o => o.sequence).ThenByDescending(o => o.docDate).ThenByDescending(o => o.updateTime).Skip(value.skip).Limit(value.limit).Project(c => new EventCalendar { 
                    code = c.code,
                    isActive = c.isActive,
                    createBy = c.createBy,
                    imageUrlCreateBy = c.imageUrlCreateBy,
                    createDate = c.createDate,
                    description = c.description,
                    descriptionEN = c.descriptionEN,
                    titleEN = c.titleEN,
                    imageUrl = c.imageUrl,
                    action = c.action,
                    title = c.title,
                    language = c.language,
                    updateBy = c.updateBy,
                    updateDate = c.updateDate,
                    sequence = c.sequence,
                    category = c.category,
                    confirmStatus = c.confirmStatus,
                    linkFacebook = c.linkFacebook,
                    linkYoutube = c.linkYoutube,
                    dateStart = c.dateStart,
                    dateEnd = c.dateEnd,
                    view = c.view,
                    linkUrl = c.linkUrl,
                    textButton = c.textButton,
                    fileUrl = c.fileUrl,
                    status2 = c.status2 }).ToList();

                //BEGIN :update view >>>>>>>>>>>>>>>>>>>>>>>>>>>>
                if (!string.IsNullOrEmpty(value.code))
                {
                    var view = docs[0].view;

                    var doc = new BsonDocument();
                    var colUpdate = new Database().MongoClient( "training");

                    var filterUpdate = Builders<BsonDocument>.Filter.Eq("code", value.code);
                    doc = colUpdate.Find(filterUpdate).FirstOrDefault();
                    var model = BsonSerializer.Deserialize<object>(doc);
                    doc["view"] = view + 1;
                    colUpdate.ReplaceOne(filterUpdate, doc);

                    docs = col.Find(filter).SortByDescending(o => o.docDate).ThenByDescending(o => o.updateTime).Skip(value.skip).Limit(value.limit).Project(c => new EventCalendar
                    {
                        code = c.code,
                        isActive = c.isActive,
                        createBy = c.createBy,
                        imageUrlCreateBy = c.imageUrlCreateBy,
                        createDate = c.createDate,
                        description = c.description,
                        descriptionEN = c.descriptionEN,
                        titleEN = c.titleEN,
                        imageUrl = c.imageUrl,
                        action = c.action,
                        title = c.title,
                        language = c.language,
                        updateBy = c.updateBy,
                        updateDate = c.updateDate,
                        sequence = c.sequence,
                        category = c.category,
                        confirmStatus = c.confirmStatus,
                        linkFacebook = c.linkFacebook,
                        linkYoutube = c.linkYoutube,
                        dateStart = c.dateStart,
                        dateEnd = c.dateEnd,
                        view = c.view,
                        linkUrl = c.linkUrl,
                        textButton = c.textButton,
                        fileUrl = c.fileUrl,
                        status2 = c.status2
                    }).ToList();
                }
                //END :update view <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

                docs.ForEach(c => {
                    // check username duplicate

                    if (!string.IsNullOrEmpty(value.username))
                    {
                        var colTR = new Database().MongoClient<EventCalendar>("trainingRegister");
                        var filterTR = Builders<EventCalendar>.Filter.Eq("createBy", value.username) & Builders<EventCalendar>.Filter.Eq("reference", c.code);
                        var docTR = colTR.Find(filterTR).FirstOrDefault();
                        if (docTR != null)
                            c.status2 = true;
                    }
                });

                // where by date
                var filterData = new List<object>();

                if (!string.IsNullOrEmpty(value.date))
                {
                    string dateSub = value.date.Substring(0, 10);
                    string[] dateArray = dateSub.Split("-");
                    string date = dateArray[0] + dateArray[1] + dateArray[2];
                    bool dateBetween = false;

                    foreach (var c in docs)
                    {
                        dateBetween = Between(date.toDateFromString(), c.dateStart.toDateFromString(), c.dateEnd.toDateFromString());
                        if (dateBetween)
                        {
                            filterData.Add(c);
                        }
                    }

                    return new Response { status = "S", message = "success", jsonData = filterData.ToJson(), objectData = filterData };
                }


                //end where by date

                return new Response { status = "S", message = "success", jsonData = docs.ToJson(), objectData = docs, totalData = col.Find(filter).ToList().Count() };
            }
            catch (Exception ex)
            {
                return new Response { status = "E", message = ex.Message };
            }
        }

        // POST /read
        [HttpPost("mark/read")]
        public ActionResult<Response> MarkRead([FromBody] EventCalendar value)
        {
            try
            {
                if (value.year == 0) {
                    return new Response { status = "E", message = "Error input year." };
                }
                var col = new Database().MongoClient<EventCalendar>( "training");
                var filter = (Builders<EventCalendar>.Filter.Eq("status", "A") & value.filterOrganization<EventCalendar>());
                if (!string.IsNullOrEmpty(value.language)) { filter = filter & Builders<EventCalendar>.Filter.Regex("language", value.language); }

                var docs = col.Find(filter).Project(c => new { c.dateStart, c.dateEnd }).ToList();

                var json = "";
                int year = value.year;
                string mount = "";
                string day = "";
                string doc = "";
                int dayPerMount = 0;
                int yearStart = year - 1;
                int yearEnd = year + 1;
                int[] YearDaysPerMonth = new int[] { };

                for (int j = yearStart; j <= yearEnd; j++)
                {
                    year = j;
                    bool leap = IsLeapYear(year);
                    if (leap)
                        YearDaysPerMonth = new int[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                    else
                        YearDaysPerMonth = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

                    for (int k = 1; k <= 12; k++)
                    {
                        dayPerMount = YearDaysPerMonth[k - 1];
                        mount = k.ToString();
                        if (mount.Length < 2)
                            mount = "0" + k;

                        for (int l = 1; l <= dayPerMount; l++)
                        {
                            day = l.ToString();
                            if (day.Length < 2)
                                day = "0" + l;

                            bool dateBetween = false;
                            string date = year + mount + day;

                            foreach (var c in docs)
                            {
                                dateBetween = Between(date.toDateFromString(), c.dateStart.toDateFromString(), c.dateEnd.toDateFromString());
                                if (dateBetween)
                                {
                                    break;
                                }
                                //do something
                            }

                            if (dateBetween)
                                json = "\"" + year + "-" + mount + "-" + day + "\":{\"marked\":true,\"selected\":false}";
                            else
                                json = "\"" + year + "-" + mount + "-" + day + "\":{\"marked\":false,\"selected\":false}";

                            if (doc == "")
                                doc = "{" + json;
                            else
                                doc = doc + "," + json;

                        }
                    }
                }

                doc = doc + "}";

                return new Response { status = "S", message = "Success", jsonData = doc };
            }
            catch (Exception ex)
            {
                return new Response { status = "E", message = ex.Message };
            }
        }

        // POST /read
        [HttpPost("gallery/read")]
        public ActionResult<Response> GalleryRead([FromBody] Criteria value)
        {
            try
            {
                var col = new Database().MongoClient<Gallery>( "trainingGallery");

                var filter = Builders<Gallery>.Filter.Eq(x => x.isActive, true);
                if (!string.IsNullOrEmpty(value.code)) { filter = filter & Builders<Gallery>.Filter.Regex("reference", value.code); }
                //filter = filter & (Builders<BsonDocument>.Filter.Eq(x => x.B, "4") | Builders<User>.Filter.Eq(x => x.B, "5"));

                var docs = col.Find(filter).Skip(value.skip).Limit(value.limit).Project(c => new { c.imageUrl }).ToList();

                //var list = new List<object>();
                //docs.ForEach(doc => { list.Add(BsonSerializer.Deserialize<object>(doc)); });
                return new Response { status = "S", message = "success", jsonData = docs.ToJson(), objectData = docs };
            }
            catch (Exception ex)
            {
                return new Response { status = "E", message = ex.Message };
            }
        }

        // POST /read
        [HttpPost("comment/read")]
        public ActionResult<Response> CommentRead([FromBody] Criteria value)
        {
            try
            {
                var col = new Database().MongoClient<Comment>("trainingComment");

                var filter = Builders<Comment>.Filter.Eq(x => x.isActive, true);
                if (!string.IsNullOrEmpty(value.code)) { filter = filter & Builders<Comment>.Filter.Regex("reference", value.code); }
                //filter = filter & (Builders<BsonDocument>.Filter.Eq(x => x.B, "4") | Builders<User>.Filter.Eq(x => x.B, "5"));

                var docs = col.Find(filter).SortByDescending(o => o.docDate).ThenByDescending(o => o.docTime).Skip(value.skip).Limit(value.limit).Project(c => new { c.description, c.createBy, c.createDate, c.imageUrlCreateBy }).ToList();

                //var list = new List<object>();
                //docs.ForEach(doc => { list.Add(BsonSerializer.Deserialize<object>(doc)); });
                return new Response { status = "S", message = "success", jsonData = docs.ToJson(), objectData = docs };
            }
            catch (Exception ex)
            {
                return new Response { status = "E", message = ex.Message };
            }
        }

        // POST /create
        [HttpPost("comment/create")]
        public ActionResult<Response> CommentCreate([FromBody] Comment value)
        {
            value.code = "".toCode();
            var doc = new BsonDocument();
            try
            {
                var word = value.description.verifyRude();
                var col = new Database().MongoClient("trainingComment");

                //check duplicate
                var filter = Builders<BsonDocument>.Filter.Eq("code", value.code);
                if (col.Find(filter).Any())
                {
                    return new Response { status = "E", message = $"code: {value.code} is exist", jsonData = value.ToJson(), objectData = value };
                }

                doc = new BsonDocument
                {
                    { "code", value.code },
                    { "description", word },
                    { "original", value.description },
                    { "imageUrlCreateBy", value.imageUrlCreateBy },
                    { "createBy", value.createBy },
                    { "createDate", DateTime.Now.toStringFromDate() },
                    { "createTime", DateTime.Now.toTimeStringFromDate() },
                    { "updateBy", value.updateBy },
                    { "updateDate", DateTime.Now.toStringFromDate() },
                    { "updateTime", DateTime.Now.toTimeStringFromDate() },
                    { "docDate", DateTime.Now.Date.AddHours(7) },
                    { "docTime", DateTime.Now.toTimeStringFromDate() },
                    { "isActive", true },
                    { "reference", value.reference }
                };
                col.InsertOne(doc);

                return new Response { status = "S", message = "success", jsonData = doc.ToJson(), objectData = BsonSerializer.Deserialize<object>(doc) };
            }
            catch (Exception ex)
            {
                return new Response { status = "E", message = ex.Message, jsonData = doc.ToJson(), objectData = BsonSerializer.Deserialize<object>(doc) };
            }
        }

        #region category

        // POST /read
        [HttpPost("category/read")]
        public ActionResult<Response> CategoryRead([FromBody] Criteria value)
        {
            try
            {
                var col = new Database().MongoClient<Category>( "trainingCategory");

                var filter = Builders<Category>.Filter.Eq(x => x.status, "A");
                if (!string.IsNullOrEmpty(value.keySearch))
                {
                    filter = Builders<Category>.Filter.Regex("title", new BsonRegularExpression(string.Format(".*{0}.*", value.keySearch), "i")) | (filter & Builders<Category>.Filter.Regex("description", new BsonRegularExpression(string.Format(".*{0}.*", value.keySearch), "i")));
                }
                else
                {
                    if (!string.IsNullOrEmpty(value.code)) { filter = filter & Builders<Category>.Filter.Regex("code", value.code); }
                    if (!string.IsNullOrEmpty(value.title)) { filter = filter & Builders<Category>.Filter.Regex("title", new BsonRegularExpression(string.Format(".*{0}.*", value.title), "i")); }
                    if (!string.IsNullOrEmpty(value.description)) { filter = filter & Builders<Category>.Filter.Regex("description", new BsonRegularExpression(string.Format(".*{0}.*", value.description), "i")); }
                    if (!string.IsNullOrEmpty(value.language)) { filter = filter & Builders<Category>.Filter.Regex("language", value.language); }
                    //if (!string.IsNullOrEmpty(value.startDate)) { filter = filter & Builders<eventCalendarCategory>.Filter.Regex("dateStart", value.startDate); }
                    //if (!string.IsNullOrEmpty(value.endDate)) { filter = filter & Builders<eventCalendarCategory>.Filter.Regex("dateEnd", value.endDate); }

                    var ds = value.startDate.toDateFromString().toBetweenDate();
                    var de = value.endDate.toDateFromString().toBetweenDate();
                    if (value.startDate != "Invalid date" && value.endDate != "Invalid date" && !string.IsNullOrEmpty(value.startDate) && !string.IsNullOrEmpty(value.endDate)) { filter = filter & Builders<Category>.Filter.Gt("docDate", ds.start) & Builders<Category>.Filter.Lt("docDate", de.end); }
                    else if (value.startDate != "Invalid date" && !string.IsNullOrEmpty(value.startDate)) { filter = filter & Builders<Category>.Filter.Gt("docDate", ds.start) & Builders<Category>.Filter.Lt("docDate", ds.end); }
                    else if (value.endDate != "Invalid date" && !string.IsNullOrEmpty(value.endDate)) { filter = filter & Builders<Category>.Filter.Gt("docDate", de.start) & Builders<Category>.Filter.Lt("docDate", de.end); }
                    //filter = filter & (Builders<BsonDocument>.Filter.Eq(x => x.B, "4") | Builders<User>.Filter.Eq(x => x.B, "5"));
                }

                var docs = col.Find(filter).SortBy(o => o.sequence).ThenByDescending(o => o.docDate).ThenByDescending(o => o.updateTime).Skip(value.skip).Limit(value.limit).Project(c => new { c.code, c.title, c.language, c.imageUrl, c.createBy, c.createDate, c.isActive }).ToList();

                return new Response { status = "S", message = "success", jsonData = docs.ToJson(), objectData = docs };
            }
            catch (Exception ex)
            {
                return new Response { status = "E", message = ex.Message };
            }

        }

        #endregion

        public static bool IsLeapYear(int year)
        {
            if (year % 400 == 0)
            {
                return true;
            }
            if (year % 100 == 0)
            {
                return false;
            }
            //otherwise
            return (year % 4) == 0;
        }
        public static bool Between(DateTime input, DateTime date1, DateTime date2)
        {
            return (input >= date1 && input <= date2);
        }

        // POST /create
        [HttpPost("register/create")]
        public ActionResult<Response> RegisterCreate([FromBody] Comment value)
        {
            value.code = "".toCode();
            var doc = new BsonDocument();
            try
            {
                var col = new Database().MongoClient( "trainingRegister");

                //check duplicate
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("code", value.code);
                    if (col.Find(filter).Any())
                    {
                        return new Response { status = "E", message = $"code: {value.code} is exist", jsonData = value.ToJson(), objectData = value };
                    }
                }
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("createBy", value.createBy) & Builders<BsonDocument>.Filter.Eq("reference", value.reference);
                    if (col.Find(filter).Any())
                    {
                        return new Response { status = "E", message = $"username: {value.code} is exist", jsonData = value.ToJson(), objectData = value };
                    }
                }


                doc = new BsonDocument
                {
                    { "code", value.code },
                    { "firstName", value.firstName },
                    { "lastName", value.lastName },
                    { "createBy", value.createBy },
                    { "createDate", DateTime.Now.toStringFromDate() },
                    { "createTime", DateTime.Now.toTimeStringFromDate() },
                    { "updateBy", value.updateBy },
                    { "updateDate", DateTime.Now.toStringFromDate() },
                    { "updateTime", DateTime.Now.toTimeStringFromDate() },
                    { "docDate", DateTime.Now.Date.AddHours(7) },
                    { "docTime", DateTime.Now.toTimeStringFromDate() },
                    { "isActive", true },
                    { "reference", value.reference }
                };
                col.InsertOne(doc);

                return new Response { status = "S", message = "success", jsonData = doc.ToJson(), objectData = BsonSerializer.Deserialize<object>(doc) };
            }
            catch (Exception ex)
            {
                return new Response { status = "E", message = ex.Message, jsonData = doc.ToJson(), objectData = BsonSerializer.Deserialize<object>(doc) };
            }
        }

        // POST /read
        [HttpPost("register/read")]
        public ActionResult<Response> RegisterRead([FromBody] Criteria value)
        {
            try
            {
                value.statisticsCreate("trainingRegister");
                var col = new Database().MongoClient<Comment>( "trainingRegister");

                var filter = Builders<Comment>.Filter.Eq(x => x.isActive, true);
                if (!string.IsNullOrEmpty(value.code)) { filter = filter & Builders<Comment>.Filter.Regex("reference", value.code); }
                //filter = filter & (Builders<BsonDocument>.Filter.Eq(x => x.B, "4") | Builders<User>.Filter.Eq(x => x.B, "5"));

                var docs = col.Find(filter).SortBy(o => o.sequence).ThenByDescending(o => o.updateDate).ThenByDescending(o => o.updateTime).Skip(value.skip).Limit(value.limit).Project(c => new { c.reference, c.description, c.createBy, c.createDate, c.imageUrlCreateBy, c.firstName, c.lastName }).ToList();

                //var list = new List<object>();
                //docs.ForEach(doc => { list.Add(BsonSerializer.Deserialize<object>(doc)); });
                return new Response { status = "S", message = "success", jsonData = docs.ToJson(), objectData = docs };
            }
            catch (Exception ex)
            {
                return new Response { status = "E", message = ex.Message };
            }
        }
    }

}