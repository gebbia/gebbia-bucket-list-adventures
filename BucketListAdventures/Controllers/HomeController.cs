
﻿using BucketListAdventures.Data;
using BucketListAdventures.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SearchActivities.ViewModel;
using System.Diagnostics;
using System.Linq;
using System;
using static BucketListAdventures.Models.ClimateNormals;


namespace BucketListAdventures.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserProfileRepository _repository;
        private readonly ILogger<HomeController> _logger;
        private static JArray data;

        private ApplicationRepository _repo;
        private readonly IConfiguration _config;
        private ClimateNormals climateNormals = new ClimateNormals();
        private static string travelAdvisorApiKey;
        public HomeController(ILogger<HomeController> logger, ApplicationRepository repo, IUserProfileRepository repository, IConfiguration config)
        {
            _logger = logger;
            _repo = repo;
            _repository = repository;
            _config = config;
            travelAdvisorApiKey = _config["travelAdvisorApiKey"];
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        [Route("/home/search")]
        public IActionResult Search()
        {
            SearchViewModel searchViewModel = new();
            return View(searchViewModel);
        }
        [HttpGet]
        [Route("/home/navigate")]

        [Authorize]
        public IActionResult Navigate()
        {
            SearchViewModel searchViewModel = new();
            return View(searchViewModel);
        }


        public static async Task<JObject> GetLatLong(string city)
        {
            string accessToken = "pk.eyJ1IjoiY2hhbWFuZWJhcmJhdHRpIiwiYSI6ImNsY3FqcW9rZTA2aW4zcXBoMGx2eTBwNm0ifQ.LFRkBS7N5yGXvCQ_F5cF9g";
            HttpClient clientName = new();
            string url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{city}.json?types=place,locality&country=us&access_token={accessToken}";
            HttpResponseMessage responseName = await clientName.GetAsync(url);
            string responseString = await responseName.Content.ReadAsStringAsync();
            JObject position = JObject.Parse(responseString);
            return position;
        }

        public async Task<string> GetAirPortDetails(string destination)
        {
            var client = new HttpClient();

            HttpRequestMessage request = GetHeaderRequest(destination);
            using var response = await client.SendAsync(request);
            Environment.GetCommandLineArgs();
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            JArray value = JArray.Parse(body);
            //data = value[0];
            return value[0]["code"].ToString();
        }

        public async Task<string> GetFlightDetails(string origin, string destination, DateTime startDate, int totalTravellers)
        {
            var client = new HttpClient();

            HttpRequestMessage request = GetFlightHeaderRequest(origin, destination, startDate, totalTravellers);
            using var response = await client.SendAsync(request);
            Environment.GetCommandLineArgs();
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            JToken value = JToken.Parse(body);
            //data = value[0];
            return value["search_params"]["sid"].ToString();
        }

        private HttpRequestMessage GetFlightPollHeaderRequest(string sid)
        {
            return new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://travel-advisor.p.rapidapi.com/flights/poll?sid={sid}&so='PRICE'&currency='USD'&n='15'&ns='NON_STOP,ONE_STOP'&o='0'"),
                Headers =
                    {
                        { "X-RapidAPI-Key", travelAdvisorApiKey },
                        { "X-RapidAPI-Host", "travel-advisor.p.rapidapi.com" },
                    },
            };
        }

        private HttpRequestMessage GetFlightHeaderRequest(string origin, string destination, DateTime startDate, int totalTravellers)
        {
            return new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://travel-advisor.p.rapidapi.com/flights/create-session?o1={origin}&d1={destination}&dd1={startDate.ToString("yyyy-MM-dd")}&ta={totalTravellers}&c=0"),
                Headers =
                    {
                        { "X-RapidAPI-Key", travelAdvisorApiKey },
                        { "X-RapidAPI-Host", "travel-advisor.p.rapidapi.com" },
                    },
            };
        }

        private HttpRequestMessage GetHeaderRequest(string destination)
        {
            return new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://travel-advisor.p.rapidapi.com/airports/search?query={destination}&locale=en_US"),
                Headers =
                    {
                        { "X-RapidAPI-Key", travelAdvisorApiKey },
                        { "X-RapidAPI-Host", "travel-advisor.p.rapidapi.com" },
                    },
            };
        }

        public static async Task<JArray> GetActivities(double lon, double lat)
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://travel-advisor.p.rapidapi.com/attractions/list-by-latlng?longitude={lon}&latitude={lat}&lunit=km&currency=USD&lang=en_US"),
                Headers =

                {
                    { "X-RapidAPI-Key", travelAdvisorApiKey },
                    { "X-RapidAPI-Host", "travel-advisor.p.rapidapi.com" },
                },

            };
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            JObject value = JObject.Parse(body);
            data = (JArray)value["data"];
            return data;
        }
        public static async Task<JArray> GetNavigation(double lon, double lat)
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://api.mapbox.com/directions/v5/mapbox/driving/-90.199585,38.626426;{lon},{lat}?geometries=geojson&access_token=pk.eyJ1IjoiY2hhbWFuZWJhcmJhdHRpIiwiYSI6ImNsY3FqcW9rZTA2aW4zcXBoMGx2eTBwNm0ifQ.LFRkBS7N5yGXvCQ_F5cF9g"),
            
               
            };
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            JObject value = JObject.Parse(body);
            
     
            
            data = (JArray)value["routes"];
            return data;
        }
        [HttpPost]
        [Route("/home/search")]
        public IActionResult DisplayResults(SearchViewModel searchViewModel)
        {
            Task<JObject> LatLong = GetLatLong(searchViewModel.CityName);
            JObject LatlongObject = LatLong.Result;
            double lon = (double)LatlongObject["features"][0]["geometry"]["coordinates"][0];
            double lat = (double)LatlongObject["features"][0]["geometry"]["coordinates"][1];
            Task<JArray> Activities = GetActivities(lon, lat);
            JArray activitiesObject = Activities.Result;
            ViewBag.activitiesObject = activitiesObject.Where(activity => (activity["name"] != null));

            WeatherStation closest_station = _repo.GetNearestWeatherStation(lat, lon);
            IEnumerable<MonthlyData> climateData = ReadCsvData(closest_station.station_id);
            ViewBag.climateData = climateData;

            return View();
        }
        [HttpPost]
        [Route("/home/navigate")]

        public IActionResult DisplayNavigate(SearchViewModel searchViewModel)
        {

            Task<JObject> LatLong = GetLatLong(searchViewModel.CityName);
            JObject LatlongObject = LatLong.Result;
            double lon = (double)LatlongObject["features"][0]["geometry"]["coordinates"][0];
            double lat = (double)LatlongObject["features"][0]["geometry"]["coordinates"][1];
            Task<JArray> Directions = GetNavigation(lon, lat);
            JArray directionsObject = Directions.Result;
            ViewBag.lon = lon;
            ViewBag.lat = lat;

           UserProfile userProfile = _repository.GetUserProfileByUserName(User.Identity.Name.ToString());
            if (userProfile == null || userProfile.Address == null)
            {
                //MessageBox.Show("You need a profile AND a valid home address to access navigation.");
            } else
            {
                ViewBag.Address = userProfile.Address;
                ViewBag.Name = userProfile.Name;
            }
            // Code for getting the address from the database goes here.
            
       
            string homeAddress = ViewBag.Address;
            
            Task<JObject> homeAddressLatLong = GetLatLong(homeAddress);
            JObject homeAddressLatlongObject = homeAddressLatLong.Result;
            double homeAddresslon = (double)homeAddressLatlongObject["features"][0]["geometry"]["coordinates"][0];
            double homeAddresslat = (double)homeAddressLatlongObject["features"][0]["geometry"]["coordinates"][1];
            
            
           
            ViewBag.homeAddresslon = homeAddresslon;
            ViewBag.homeAddresslat = homeAddresslat;
            ViewBag.directionsObject = directionsObject;


            return View();
        }
        [HttpGet]
        [Route("/home/details")]
        public IActionResult Details(string activity)
        {
            foreach (var activityDetail in data)
            {
                if (activity == (string)activityDetail["name"])
                {
                    ViewBag.activityDetails = activityDetail;
                    return View();
                }
            }
            return View();
        }

        [Authorize]
        [HttpGet]
        [Route("/home/searchtravellers")]
        public IActionResult SearchTravellers()
        {
            SearchTravellerViewModel SearchTravellerViewModel = new();
            SearchTravellerViewModel.StartDate = DateTime.Now;
            SearchTravellerViewModel.CurrentLocation = _repository.GetUserProfileByUserName(User.Identity.Name.ToString()).AirLineCode;
            return View(SearchTravellerViewModel);
        }

        [HttpPost]
        [Route("/home/searchtravellers")]
        public IActionResult DisplayResultsForTraveller(SearchTravellerViewModel searchTravellerViewModel)
        {
            //ViewBag.flightResults = GetFlightDetails(searchTravellerViewModel);
            ViewBag.flightResults = JToken.Parse("{\"summary\":{\"cu\":\"USD\",\"et\":1675838988,\"op\":[{\"k\":\"STL\",\"t\":\"Saint Louis, MO (STL)\",\"p\":\"$139\"}],\"so\":[{\"st\":\"Sorted by Price\",\"n\":\"Price\",\"k\":\"PRICE\"},{\"st\":\"Sorted by Duration\",\"n\":\"Duration\",\"k\":\"DURATION\"},{\"st\":\"Sorted by Best Value\",\"n\":\"Best Value\",\"dc\":\"These flights offer the best combination of price, flight duration, and sometimes factors such as additional fees.\",\"k\":\"ML_BEST_VALUE\"},{\"st\":\"Sorted by Earliest outbound departure\",\"n\":\"Earliest outbound departure\",\"k\":\"EARLIEST_OUTBOUND_DEPARTURE\"},{\"st\":\"Sorted by Earliest outbound arrival\",\"n\":\"Earliest outbound arrival\",\"k\":\"EARLIEST_OUTBOUND_ARRIVAL\"},{\"st\":\"Sorted by Latest outbound departure\",\"n\":\"Latest outbound departure\",\"k\":\"LATEST_OUTBOUND_DEPARTURE\"},{\"st\":\"Sorted by Latest outbound arrival\",\"n\":\"Latest outbound arrival\",\"k\":\"LATEST_OUTBOUND_ARRIVAL\"}],\"fi\":-1,\"pd\":\"$139\",\"dp\":[{\"k\":\"JFK\",\"t\":\"New York City, NY (JFK)\",\"p\":\"$265\"},{\"k\":\"LGA\",\"t\":\"New York City, NY (LGA)\",\"p\":\"$139\"},{\"k\":\"EWR\",\"t\":\"Newark, NJ (EWR)\",\"p\":\"$139\"}],\"ap\":[{\"k\":\"BOS\",\"t\":\"Boston, MA (BOS)\",\"p\":\"$275\"},{\"k\":\"CLT\",\"t\":\"Charlotte, NC (CLT)\",\"p\":\"$181\"},{\"k\":\"ORD\",\"t\":\"Chicago, IL (ORD)\",\"p\":\"$229\"},{\"k\":\"CVG\",\"t\":\"Cincinnati, OH (CVG)\",\"p\":\"$455\"},{\"k\":\"DFW\",\"t\":\"Dallas, TX (DFW)\",\"p\":\"$238\"},{\"k\":\"DAY\",\"t\":\"Dayton, OH (DAY)\",\"p\":\"$618\"},{\"k\":\"GSO\",\"t\":\"Greensboro, NC (GSO)\",\"p\":\"$552\"},{\"k\":\"MIA\",\"t\":\"Miami, FL (MIA)\",\"p\":\"$611\"},{\"k\":\"ORF\",\"t\":\"Norfolk, VA (ORF)\",\"p\":\"$616\"},{\"k\":\"MCO\",\"t\":\"Orlando, FL (MCO)\",\"p\":\"$188\"},{\"k\":\"PHL\",\"t\":\"Philadelphia, PA (PHL)\",\"p\":\"$787\"},{\"k\":\"RDU\",\"t\":\"Raleigh, NC (RDU)\",\"p\":\"$442\"},{\"k\":\"RIC\",\"t\":\"Richmond, VA (RIC)\",\"p\":\"$453\"},{\"k\":\"SEA\",\"t\":\"Seattle, WA (SEA)\",\"p\":\"$1,138\"},{\"k\":\"DCA\",\"t\":\"Washington DC, DC (DCA)\",\"p\":\"$149\"},{\"k\":\"IAD\",\"t\":\"Washington DC, DC (IAD)\",\"p\":\"$210\"}],\"sd\":[{\"k\":\"0\",\"min\":\"2023-03-24T10:10:00Z\",\"max\":\"2023-03-24T23:41:00Z\",\"tz\":\"America/Chicago\"}],\"sa\":[{\"k\":\"0\",\"min\":\"2023-03-24T13:25:00Z\",\"max\":\"2023-03-25T17:33:00Z\",\"tz\":\"America/New_York\"}],\"su\":[{\"k\":\"0\",\"min\":135,\"max\":1511}],\"a\":[],\"cp\":[{\"k\":\"AS\",\"t\":\"Alaska\",\"p\":\"$1,138\"},{\"k\":\"AA\",\"t\":\"American\",\"p\":\"$139\"},{\"k\":\"DL\",\"t\":\"Delta\",\"p\":\"--\"},{\"k\":\"NK\",\"t\":\"Spirit\",\"p\":\"$188\"},{\"k\":\"UA\",\"t\":\"United\",\"p\":\"$139\"}],\"ocp\":[{\"k\":\"AS\",\"t\":\"Alaska\",\"p\":\"$1,138\"},{\"k\":\"AA\",\"t\":\"American\",\"p\":\"$139\"},{\"k\":\"9E\",\"t\":\"Endeavor\",\"p\":\"--\"},{\"k\":\"YX\",\"t\":\"Republic\",\"p\":\"--\"},{\"k\":\"NK\",\"t\":\"Spirit\",\"p\":\"$188\"},{\"k\":\"UA\",\"t\":\"United\",\"p\":\"$139\"},{\"k\":\"ZZ\",\"t\":\"Multiple Airlines\",\"p\":\"$275\"}],\"pp\":[{\"k\":\"American Airlines\",\"t\":\"American Airlines\",\"p\":\"$139\"},{\"k\":\"CheapOair\",\"t\":\"CheapOair\",\"p\":\"$188\"},{\"k\":\"Delta\",\"t\":\"Delta\",\"p\":\"--\"},{\"k\":\"eDreams\",\"t\":\"eDreams\",\"p\":\"$192\"},{\"k\":\"FlightHub\",\"t\":\"FlightHub\",\"p\":\"$188\"},{\"k\":\"JustFly\",\"t\":\"JustFly\",\"p\":\"$188\"},{\"k\":\"Priceline\",\"t\":\"Priceline\",\"p\":\"$196\"},{\"k\":\"Spirit Airlines\",\"t\":\"Spirit Airlines\",\"p\":\"$196\"},{\"k\":\"United Airlines\",\"t\":\"United Airlines\",\"p\":\"$139\"}],\"sp\":[{\"k\":\"NON_STOP\",\"t\":\"Nonstop\",\"p\":\"$139\"},{\"k\":\"ONE_STOP\",\"t\":\"1 Stop\",\"p\":\"$149\"},{\"k\":\"TWO_PLUS\",\"t\":\"2+ Stops\",\"p\":\"$442\"}],\"dn\":135,\"dx\":1511,\"da\":502.8611145019531,\"c\":false,\"f\":false,\"p\":416.7,\"sh\":\"cffb1e13d6314f38445f40b4a84f9cc0\",\"nr\":144},\"itineraries\":[{\"key\":\"00AAAA0.1407STL0.1630LGA0\",\"ac\":\"00AAAA0.1407STL0.1630LGA0\",\"l\":[{\"pr\":{\"p\":138.90001,\"f\":0.0,\"dp\":\"$139\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|27\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|AA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"LGA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Embraer 175\",\"f\":\"4743\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T09:07:00-05:00\",\"ad\":\"2023-03-24T12:30:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":886.8678}],\"key\":\"00AAAA0.1407STL0.1630LGA0\",\"lo\":[],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"},{\"key\":\"00AAAA0.2214STL1.0037LGA0\",\"ac\":\"00AAAA0.2214STL1.0037LGA0\",\"l\":[{\"pr\":{\"p\":138.90001,\"f\":0.0,\"dp\":\"$139\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|13\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|AA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"LGA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Embraer 170\",\"f\":\"4622\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T17:14:00-05:00\",\"ad\":\"2023-03-24T20:37:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":886.8678}],\"key\":\"00AAAA0.2214STL1.0037LGA0\",\"lo\":[],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"},{\"key\":\"00AAAA0.1103STL0.1309DCA0~00AAAA0.1611DCA0.1720LGA0\",\"ac\":\"00AAAA0.1103STL0.1309DCA0~00AAAA0.1611DCA0.1720LGA0\",\"l\":[{\"pr\":{\"p\":148.2,\"f\":0.0,\"dp\":\"$149\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|12\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|AA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"DCA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Embraer 175\",\"f\":\"4795\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T06:03:00-05:00\",\"ad\":\"2023-03-24T09:09:00-04:00\",\"tt\":\"f\",\"stf\":false,\"od\":\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\",\"ac\":[],\"di\":717.9033},{\"da\":\"DCA\",\"aa\":\"LGA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Airbus A319\",\"f\":\"2119\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T12:11:00-04:00\",\"ad\":\"2023-03-24T13:20:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":214.52243}],\"key\":\"00AAAA0.1103STL0.1309DCA0~00AAAA0.1611DCA0.1720LGA0\",\"lo\":[{\"s\":\"DCA\",\"d\":182,\"t\":\"Layover at DCA for <b>3h 2m</b>\",\"c\":\"NORMAL\"}],\"od\":[\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\"]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\"},{\"key\":\"00AAAA0.2341STL1.0127CLT0~00AAAA1.0202CLT1.0345LGA0\",\"ac\":\"00AAAA0.2341STL1.0127CLT0~00AAAA1.0202CLT1.0345LGA0\",\"l\":[{\"pr\":{\"p\":180.7,\"f\":0.0,\"dp\":\"$181\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|25\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|AA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"CLT\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Airbus A319\",\"f\":\"2937\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T18:41:00-05:00\",\"ad\":\"2023-03-24T21:27:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":574.7311},{\"da\":\"CLT\",\"aa\":\"LGA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Airbus A321-100/200\",\"f\":\"2031\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T22:02:00-04:00\",\"ad\":\"2023-03-24T23:45:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":544.361}],\"key\":\"00AAAA0.2341STL1.0127CLT0~00AAAA1.0202CLT1.0345LGA0\",\"lo\":[{\"s\":\"CLT\",\"d\":35,\"t\":\"Short layover at CLT for <b>0h 35m</b>\",\"c\":\"SHORT\"}],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"},{\"key\":\"00AAAA0.1103STL0.1309DCA0~00AAAA0.1400DCA0.1507LGA0\",\"ac\":\"00AAAA0.1103STL0.1309DCA0~00AAAA0.1400DCA0.1507LGA0\",\"l\":[{\"pr\":{\"p\":182.2,\"f\":0.0,\"dp\":\"$183\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|17\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|AA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"DCA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Embraer 175\",\"f\":\"4795\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T06:03:00-05:00\",\"ad\":\"2023-03-24T09:09:00-04:00\",\"tt\":\"f\",\"stf\":false,\"od\":\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\",\"ac\":[],\"di\":717.9033},{\"da\":\"DCA\",\"aa\":\"LGA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Airbus A319\",\"f\":\"2030\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T10:00:00-04:00\",\"ad\":\"2023-03-24T11:07:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":214.52243}],\"key\":\"00AAAA0.1103STL0.1309DCA0~00AAAA0.1400DCA0.1507LGA0\",\"lo\":[{\"s\":\"DCA\",\"d\":51,\"t\":\"Layover at DCA for <b>0h 51m</b>\",\"c\":\"NORMAL\"}],\"od\":[\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\"]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\"},{\"key\":\"00AAAA0.1103STL0.1309DCA0~00AAAA0.1700DCA0.1818LGA0\",\"ac\":\"00AAAA0.1103STL0.1309DCA0~00AAAA0.1700DCA0.1818LGA0\",\"l\":[{\"pr\":{\"p\":182.2,\"f\":0.0,\"dp\":\"$183\",\"df\":\"$0.00\"},\"id\":\"Kayak|6|73\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|AA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"DCA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Embraer 175\",\"f\":\"4795\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T06:03:00-05:00\",\"ad\":\"2023-03-24T09:09:00-04:00\",\"tt\":\"f\",\"stf\":false,\"od\":\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\",\"ac\":[],\"di\":717.9033},{\"da\":\"DCA\",\"aa\":\"LGA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Embraer 175\",\"f\":\"4383\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T13:00:00-04:00\",\"ad\":\"2023-03-24T14:18:00-04:00\",\"tt\":\"f\",\"stf\":false,\"od\":\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\",\"ac\":[],\"di\":214.52243}],\"key\":\"00AAAA0.1103STL0.1309DCA0~00AAAA0.1700DCA0.1818LGA0\",\"lo\":[{\"s\":\"DCA\",\"d\":231,\"t\":\"Long layover at DCA for <b>3h 51m</b>\",\"c\":\"LONG\"}],\"od\":[\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\"]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"REPUBLIC AIRWAYS AS AMERICAN EAGLE\"},{\"key\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.1022MCO1.1304EWR0\",\"ac\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.1022MCO1.1304EWR0\",\"l\":[{\"pr\":{\"p\":187.78001,\"f\":0.0,\"dp\":\"$188\",\"df\":\"$0.00\"},\"id\":\"Kayak|2|6\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|CHEAPOAIR\",\"pl\":[]},{\"pr\":{\"p\":187.79,\"f\":0.0,\"dp\":\"$188\",\"df\":\"$0.00\"},\"id\":\"Kayak|4|6\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|FLIGHTHUBUS\",\"pl\":[]},{\"pr\":{\"p\":187.79,\"f\":0.0,\"dp\":\"$188\",\"df\":\"$0.00\"},\"id\":\"Kayak|4|5\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|FLIGHTHUB\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"MCO\",\"c\":0,\"m\":\"NK\",\"o\":\"NK\",\"e\":\"Airbus A320 (sharklets)\",\"f\":\"1130\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T15:05:00-05:00\",\"ad\":\"2023-03-24T18:36:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":884.0837},{\"da\":\"MCO\",\"aa\":\"EWR\",\"c\":0,\"m\":\"NK\",\"o\":\"NK\",\"e\":\"Airbus A321 (sharklets)\",\"f\":\"59\",\"si\":0,\"n\":0,\"dd\":\"2023-03-25T06:22:00-04:00\",\"ad\":\"2023-03-25T09:04:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":940.3704}],\"key\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.1022MCO1.1304EWR0\",\"lo\":[{\"s\":\"MCO\",\"d\":706,\"t\":\"Long layover at MCO for <b>11h 46m</b>\",\"c\":\"LONG\"}],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"},{\"key\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.0025MCO1.0305LGA0\",\"ac\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.0025MCO1.0305LGA0\",\"l\":[{\"pr\":{\"p\":191.8,\"f\":0.0,\"dp\":\"$192\",\"df\":\"$0.00\"},\"id\":\"Kayak|5|1\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|EDREAMSAIRAFFILIATE\",\"pl\":[]},{\"pr\":{\"p\":195.18,\"f\":0.0,\"dp\":\"$196\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|3\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|FLIGHTHUBUS\",\"pl\":[]},{\"pr\":{\"p\":195.18,\"f\":0.0,\"dp\":\"$196\",\"df\":\"$0.00\"},\"id\":\"Kayak|2|1\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|CHEAPOAIR\",\"pl\":[]},{\"pr\":{\"p\":195.18,\"f\":0.0,\"dp\":\"$196\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|2\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|NK\",\"pl\":[]},{\"pr\":{\"p\":195.18,\"f\":0.0,\"dp\":\"$196\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|4\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|FLIGHTHUB\",\"pl\":[]},{\"pr\":{\"p\":195.23,\"f\":0.0,\"dp\":\"$196\",\"df\":\"$0.00\"},\"id\":\"Kayak|4|1\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|PRICELINEFLIGHTS\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"MCO\",\"c\":0,\"m\":\"NK\",\"o\":\"NK\",\"e\":\"Airbus A320 (sharklets)\",\"f\":\"1130\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T15:05:00-05:00\",\"ad\":\"2023-03-24T18:36:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":884.0837},{\"da\":\"MCO\",\"aa\":\"LGA\",\"c\":0,\"m\":\"NK\",\"o\":\"NK\",\"e\":\"Airbus A320 (sharklets)\",\"f\":\"686\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T20:25:00-04:00\",\"ad\":\"2023-03-24T23:05:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":952.88544}],\"key\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.0025MCO1.0305LGA0\",\"lo\":[{\"s\":\"MCO\",\"d\":109,\"t\":\"Layover at MCO for <b>1h 49m</b>\",\"c\":\"NORMAL\"}],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"},{\"key\":\"00AAAA0.1030STL0.1218CLT0~00AAAA0.1615CLT0.1759LGA0\",\"ac\":\"00AAAA0.1030STL0.1218CLT0~00AAAA0.1615CLT0.1759LGA0\",\"l\":[{\"pr\":{\"p\":202.7,\"f\":0.0,\"dp\":\"$203\",\"df\":\"$0.00\"},\"id\":\"Kayak|6|27\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|AA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"CLT\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Airbus A320-100/200\",\"f\":\"1515\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T05:30:00-05:00\",\"ad\":\"2023-03-24T08:18:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":574.7311},{\"da\":\"CLT\",\"aa\":\"LGA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Boeing 737-800\",\"f\":\"2902\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T12:15:00-04:00\",\"ad\":\"2023-03-24T13:59:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":544.361}],\"key\":\"00AAAA0.1030STL0.1218CLT0~00AAAA0.1615CLT0.1759LGA0\",\"lo\":[{\"s\":\"CLT\",\"d\":237,\"t\":\"Long layover at CLT for <b>3h 57m</b>\",\"c\":\"LONG\"}],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"},{\"key\":\"00UAUA0.2305STL1.0109IAD0~00UAUA1.0210IAD1.0332EWR0\",\"ac\":\"00UAUA0.2305STL1.0109IAD0~00UAUA1.0210IAD1.0332EWR0\",\"l\":[{\"pr\":{\"p\":209.09999,\"f\":0.0,\"dp\":\"$210\",\"df\":\"$0.00\"},\"id\":\"Kayak|3|14\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|UA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"IAD\",\"c\":0,\"m\":\"UA\",\"o\":\"UA\",\"e\":\"Embraer ERJ-135 / ERJ-140 / ERJ-145\",\"f\":\"4833\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T18:05:00-05:00\",\"ad\":\"2023-03-24T21:09:00-04:00\",\"tt\":\"f\",\"stf\":false,\"od\":\"COMMUTEAIR DBA UNITED EXPRESS\",\"ac\":[],\"di\":695.1444},{\"da\":\"IAD\",\"aa\":\"EWR\",\"c\":0,\"m\":\"UA\",\"o\":\"UA\",\"e\":\"Boeing 737-800\",\"f\":\"596\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T22:10:00-04:00\",\"ad\":\"2023-03-24T23:32:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":211.70345}],\"key\":\"00UAUA0.2305STL1.0109IAD0~00UAUA1.0210IAD1.0332EWR0\",\"lo\":[{\"s\":\"IAD\",\"d\":61,\"t\":\"Layover at IAD for <b>1h 1m</b>\",\"c\":\"NORMAL\"}],\"od\":[\"COMMUTEAIR DBA UNITED EXPRESS\"]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"COMMUTEAIR DBA UNITED EXPRESS\"},{\"key\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.0900MCO1.1140LGA0\",\"ac\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.0900MCO1.1140LGA0\",\"l\":[{\"pr\":{\"p\":210.78001,\"f\":0.0,\"dp\":\"$211\",\"df\":\"$0.00\"},\"id\":\"Kayak|2|3\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|CHEAPOAIR\",\"pl\":[]},{\"pr\":{\"p\":210.79,\"f\":0.0,\"dp\":\"$211\",\"df\":\"$0.00\"},\"id\":\"Kayak|4|2\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|FLIGHTHUBUS\",\"pl\":[]},{\"pr\":{\"p\":210.79,\"f\":0.0,\"dp\":\"$211\",\"df\":\"$0.00\"},\"id\":\"Kayak|4|3\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|FLIGHTHUB\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"MCO\",\"c\":0,\"m\":\"NK\",\"o\":\"NK\",\"e\":\"Airbus A320 (sharklets)\",\"f\":\"1130\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T15:05:00-05:00\",\"ad\":\"2023-03-24T18:36:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":884.0837},{\"da\":\"MCO\",\"aa\":\"LGA\",\"c\":0,\"m\":\"NK\",\"o\":\"NK\",\"e\":\"Airbus A321 (sharklets)\",\"f\":\"316\",\"si\":0,\"n\":0,\"dd\":\"2023-03-25T05:00:00-04:00\",\"ad\":\"2023-03-25T07:40:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":952.88544}],\"key\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.0900MCO1.1140LGA0\",\"lo\":[{\"s\":\"MCO\",\"d\":624,\"t\":\"Long layover at MCO for <b>10h 24m</b>\",\"c\":\"LONG\"}],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"},{\"key\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.1453MCO1.1733LGA0\",\"ac\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.1453MCO1.1733LGA0\",\"l\":[{\"pr\":{\"p\":210.79,\"f\":0.0,\"dp\":\"$211\",\"df\":\"$0.00\"},\"id\":\"Kayak|6|82\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|FLIGHTHUB\",\"pl\":[]},{\"pr\":{\"p\":210.79,\"f\":0.0,\"dp\":\"$211\",\"df\":\"$0.00\"},\"id\":\"Kayak|6|83\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|FLIGHTHUBUS\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"MCO\",\"c\":0,\"m\":\"NK\",\"o\":\"NK\",\"e\":\"Airbus A320 (sharklets)\",\"f\":\"1130\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T15:05:00-05:00\",\"ad\":\"2023-03-24T18:36:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":884.0837},{\"da\":\"MCO\",\"aa\":\"LGA\",\"c\":0,\"m\":\"NK\",\"o\":\"NK\",\"e\":\"Airbus A320 (sharklets)\",\"f\":\"1351\",\"si\":0,\"n\":0,\"dd\":\"2023-03-25T10:53:00-04:00\",\"ad\":\"2023-03-25T13:33:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":952.88544}],\"key\":\"00NKNK0.2005STL0.2236MCO0~00NKNK1.1453MCO1.1733LGA0\",\"lo\":[{\"s\":\"MCO\",\"d\":977,\"t\":\"Long layover at MCO for <b>16h 17m</b>\",\"c\":\"LONG\"}],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"}],\"inserts\":{\"inline\":[{\"cp\":\"MAUrl=%2F%2Ftravel.mediaalpha.com%2Fclick.html%3Fcu%3Dhttps%253A%252F%252Fwww.cheapflights.com%252Fsemi%252Fmalpha%252Fflight_destination%252FSTL.NYC%252Fen.html%253Fd1%253D2023-03-24%2526pa%253D3%2526ft%253Dow%2526cmpid%253D1273%2526agid%253D360361%2526ad%253D25747390%2526pbid%253D205%2526rk%253D2%2526cnl%253D1996%2526ts%253D1675837316%2526b%253Df%2526pt%253Dflight%2526d%253Dm%2526cmp%253Ddefault%2526dta%253D45%2526dur%253D1%2526utm_source%253Dmediaalpha%2526utm_medium%253Dcpc%2526utm_campaign%253DF%252520-%252520US%252520-%252520Flights%252520-%252520Mobile%252520-%252520OEX%2526utm_content%253DUnited%252520States_flight_d_ow_STL-NYC%2526token%253DAAAFAUWIUA2MQQUV74UYZI57H2QGIAGTN4%26si%3D4812154067005624320%26paci%3D5%26ad_pos%3D2%26ad_num%3D%7Btotal_ads%7D%3B_HikB55i_RAytupgRk3vj0ZmXoe42hyFnVoL7HbrkCHSAFFqG9Bhd_v4pQRwKR2SdRapthFUfbICM1XYmiXwqz5pFt_Wsj282p083Tv7koEufaCEqDVaEsxXDWf5f4ptk6cXBd37KUgWpGe1lSGP6oZJsksgNtPhhkH68mdIXcaR4QlIAio9RGRjhLMfEHi96OU_3gT6nA9ru4ItVSZD0iwomM8_Czrih4poytrMeMiLR_TsGm55pUJD7AQhIFOZTiAWbO4HTmcifhijN5TEit7cKWESblQvhcEy4t9rPBgv0ZZTtiiilktdIPub2Pn2aLz14zcrg33naBpRiRPYyVI3TQuPESq6YwmYUdqZwpUywHiT4J4HZwGxK377hcyslD1NkIg_3AzqueSR-lzjoR30Cz6AlJSvGX5CjSMsiElvFEybuHFc5T9WnXVYusQpeUl7xfefTwNTPgc9qIE12yXrm51fGAvvn-fHwCZ1vWb_5F_Zj6_tTuzGvI0s2x3ATp5JfhG0nVCiTv1LTH4v7SN7eCMuTt2ly09gneeBrSe4WsjZtLhXCBrSSxaXv6t7IiuBEXZKfbRt66nJHVZoP9sB9eSiaI2PckjjiajAqIyxOtEvZnPmy-LJrYrRSTF05fwF0mRdyRFDr9yxWzR7yAK9AM2YwFlqXYbqdjh7Of78vIOuJenxZzGyseH0X1OGFe5H8my0wEeOTFweux_98WQ4viwmSPO8nCKzAlLJpdTZa9JZat1h43xnhxG5S4SPSFHi9r_w_iyQijTM77hHwx7h_Qd-RiVWOVq_gkjHr4eVVgnzhGiFk5Ja4Q365GktyxphqDdxwb8-JzSIgYzqBeX9DoxmIw&Dest=NYC&Orig=STL&cos=0&inYear=2023&adults=3&cnt=1&clt=a&gosox=StDXtZooNRUFQRlxMNKTp9qTFJV4dKiri5uHedVyoRpf4As7KO1dwraV6I2cnnOnoWwZ-NueYGbt0ix_-RC0IQ&slot=1&silo=36357&inDay=24&inMonth=03&sid=364aa989-6bab-4c0e-bc19-57c0fda1fd45.146&buyer=CheapFlights&bucket=923259&geo=60763&AirProvider=MediaAlphaFlightsMerch&oi=1728\",\"sp\":\"Sponsored\",\"lu\":\"https:////d29u10q7qlh006.cloudfront.net/t/i/182/aKanxstjTiGNInuNVyPPJOrOEGE.png\",\"oi\":1728,\"r\":3,\"l1\":\"Cheap flights from Saint Louis to New York City\",\"l2\":\"Get inspired, and find the best deals!\",\"ct\":\"View Deal\",\"pv\":\"MediaAlphaGlobalInlineMeta\",\"ps\":\"TOP\"},{\"cp\":\"MAUrl=%2F%2Ftravel.mediaalpha.com%2Fclick.html%3Fcu%3Dhttps%253A%252F%252Fservedby.flashtalking.com%252Fclick%252F8%252F199533%253B7066568%253B50126%253B211%253B0%252F%253Fft_width%253D1%2526ft_height%253D1%2526url%253D35323722%26si%3D4812154067005624320%26paci%3D5%26ad_pos%3D1%26ad_num%3D%7Btotal_ads%7D%3B_HikB55i_RAytupgRk3vj0ZmXoe42hyFnVoL7HbrkCHSAFFqG9Bhd_v4pQRwKR2SdRapthFUfbICM1XYmiXwqz5pFt_Wsj282p083Tv7koEufaCEqDVaEsxXDWf5f4ptk6cXBd37KUgWpGe1lSGP6oZJsksgNtPhhkH68mdIXcaR4QlIAio9RGRjhLMfEHi96OU_3gT6nA9ru4ItVSZD0iwomM8_Czrih4poytrMeMiLR_TsGm55pUJD7AQhIFOZTiAWbO4HTmcifhijN5TEijqO2GSv4N2PAegXwQ8TpIPkqwbaZHdlYVUzZcroJpq8Konx6TxxrLah9s-6plB1POB4C9hnMM4hithig0K8t72VhsXfpnL9fbnnf-1OMjkXLdrfma1GHrLz-suL9VopIxrXwOT4qTBNaXvmpiT572g5eca502aD-qVVsus_YiplDccTQ4WCWqUlH54vYDRBOxZ3No5ahCNsNvykaD88_JS57hBOg_ZqTCQwhjRJ_bxfjuq9U_1z7Z4DU_0WT3by9VrwL2vOYPb6GnJy-hcg4XU3w1oBxSLvC4dOcUgfSnDe2bD_WogNtvyWPJhPxAppsLHopwzNqGH1AnbDW59a-yniIBTxulvzc0HEb9zMzr1G4s8ODQ6467EIyfk0y6Puh2L0D2K8znsepqcbGCFO9d3DP0EHkfZM6s6AiXJFsB188ahd6wyGHsBTrq0Px9z7e6Fx8XmmP1VWLXspjdI9MV9EvmLk34yfsjxh5ulDWQ3-rFVnTUnOAtfT_YOH4Dhn0jwDyoUDeArqQVCdmh3G15IL8gtER0UElnQ19mfCHJ3Xa_Fyz7cEG6wSqk9qHzU1B2ubeF3xf87jvFkUtHIAxSczSU3P-jQ&Dest=NYC&Orig=STL&cos=0&inYear=2023&adults=3&cnt=1&clt=a&gosox=StDXtZooNRUFQRlxMNKTp9qTFJV4dKiri5uHedVyoRqs4oI-o3MNMCZLrjxiOIWNXPscv34RxpP1mJffkEq1rQ&slot=1&silo=36357&inDay=24&inMonth=03&sid=364aa989-6bab-4c0e-bc19-57c0fda1fd45.146&buyer=Southwest+Airlines&bucket=923259&geo=60763&AirProvider=MediaAlphaFlightsMerch&oi=1728\",\"sp\":\"Sponsored\",\"lu\":\"https:////d29u10q7qlh006.cloudfront.net/t/i/699/TJSuvWxPmI8wAGHGUvlgHMvE-S8.png\",\"oi\":1728,\"r\":8,\"l1\":\"First Two Bags Fly Free® With Us\",\"l2\":\"<ul><li>Book your next trip to New York City on Southwest.com®. Weight &amp; Size Limits Apply.</li></ul>\",\"ct\":\"View Deal\",\"pv\":\"MediaAlphaGlobalInlineMeta\",\"ps\":\"TOP\"}],\"center_column\":[{\"cp\":\"MAUrl=%2F%2Ftravel.mediaalpha.com%2Fclick.html%3Fcu%3Dhttps%253A%252F%252Fwww.cheapflights.com%252Fsemi%252Fmalpha%252Fflight_destination%252FSTL.NYC%252Fen.html%253Fd1%253D2023-03-24%2526pa%253D3%2526ft%253Dow%2526cmpid%253D1273%2526agid%253D360361%2526ad%253D25747390%2526pbid%253D205%2526rk%253D2%2526cnl%253D1995%2526ts%253D1675837316%2526b%253Df%2526pt%253Dflight%2526d%253Dm%2526cmp%253Ddefault%2526dta%253D45%2526dur%253D1%2526utm_source%253Dmediaalpha%2526utm_medium%253Dcpc%2526utm_campaign%253DF%252520-%252520US%252520-%252520Flights%252520-%252520Mobile%252520-%252520OEX%2526utm_content%253DUnited%252520States_flight_d_ow_STL-NYC%2526token%253DAAAKQTOMUA2MQQUV74UYZI57H2QFYAEZ7U%26si%3D4812154067072428032%26paci%3D5%26ad_pos%3D2%26ad_num%3D%7Btotal_ads%7D%3B_HikB55i_RAytupgRk3vjxBlvAoq64C4gUNXdh4DUG6xTOl54zc0LSwIRxTBqLZjqMac9wu346qhcneP3XIeB6n10Zh9VtIh3P_G2Z224ONON79A7E0J6I77QX9_TxR5ul7sLfPLymKGUPDoDCYebOYjAvTJhn_X3SSB7xus4WLvXptoBKnmF3f82Z9unGOr-Ylb34dxGVWOHsJ9Q2_niAhHKLuegZRdf-tk9BkiDUgNekcj0f3WF4_3szoSqkRg8Mwwco1BBALcQ_8JQ04kVA9MKXGOKhSwyMNvjEWVOxzKPpZ5tI_2FK-pHpQB_5G8iusEvP-Rxfa3V5eeo2wHzyhjl9FUifzOuwzfg6z_Ql-0UQMEnn3DUK_9ai8N5zQzwF3K_sHKx6n4OGUu5a0eMotR1_SG-mSWLqyxrSwJ0_tff4tz6FpLDq0wr1yblO_aZIH3sVv2_N4jnQtOvZeEnrXQ4dXvYsDQmNgLAGx5g1CgHs_rh8mKNaWT8KIVIhhK8JLoju6484HF1Q0GtBJprgDGvWAPfj-NCbX47Hcggd9iaAC_uwSqUSntv8m9O_dGcrktbonq_qYYe5na9dykA-PT3Vj0tFok77uI_mBkd1DtrwHrVgQBplTRPUvYOvl4BOxxoyU5_jILrsuCV76C1vdrvSgBiaW9MLa5OkXSVpJHjS3XXJMs90LK5cnaDpo1Yyv-IK7jegiUrmHAfSHJAFjQJqjFpRN3KlaD4Vn5fYBGYEeuVWxVKNOyFQo258F2OVXekpSKMnHUw0k5SyHJ9_OU0eDWD90UvdXJTL-RTtbWkJrvOFwraLdqrhQCBHHYJ7IbW4qI3GXLpKEd4qtqEqAne_vPqw&Dest=NYC&Orig=STL&cos=0&inYear=2023&adults=3&cnt=1&clt=a&gosox=We1bfJ5I6Lg4yp5ziGJMY_pgLXuwc012Kap2v-Piwod_v_roG2K0eYD0DWIRbMQiF0b7EJRjpscI-IFl6JG1Ag&slot=1&silo=36356&inDay=24&inMonth=03&sid=364aa989-6bab-4c0e-bc19-57c0fda1fd45.146&buyer=CheapFlights&bucket=923258&geo=60763&AirProvider=MediaAlphaFlightsMerch&oi=1727\",\"sp\":\"Sponsored\",\"lu\":\"https:////d29u10q7qlh006.cloudfront.net/t/i/182/aKanxstjTiGNInuNVyPPJOrOEGE.png\",\"oi\":1727,\"r\":0,\"l1\":\"Cheap flights from Saint Louis to New York City\",\"l2\":\"Get inspired, and find the best deals!\",\"ct\":\"View Deal\",\"pv\":\"MediaAlphaGlobalCC\",\"ps\":\"TOP\"}]},\"carriers\":[{\"i\":8729020,\"c\":\"AA\",\"l\":\"https://static.tacdn.com/img2/flights/airlines/logos/100x100/AmericanAirlines.png\",\"n\":\"American\"},{\"i\":0,\"c\":\"ZZ\",\"l\":\"https://static.tacdn.com/img2/flights/airlines/logos/100x100/api_default.png\",\"n\":\"Multiple Airlines\"},{\"i\":8729177,\"c\":\"UA\",\"l\":\"https://static.tacdn.com/img2/flights/airlines/logos/100x100/UnitedAirlines.png\",\"n\":\"United\"},{\"i\":8729157,\"c\":\"NK\",\"l\":\"https://static.tacdn.com/img2/flights/airlines/logos/100x100/Spirit.png\",\"n\":\"Spirit\"}],\"providers\":[{\"l\":\"https://content.r9cdn.net/rimg/provider-logos/airlines/h/UA.png?crop=false&width=166&height=62&fallback=default1.png&_v=7c677aa11f74e1ce48f12499d795a403\",\"n\":\"United Airlines\",\"i\":\"United Airlines\"},{\"l\":\"https://content.r9cdn.net/rimg/provider-logos/airlines/h/EDREAMSAIR.us.png?crop=false&width=166&height=62&fallback=default2.png&_v=af0479a857c0eb142b1531d305bbf971\",\"n\":\"eDreams\",\"i\":\"eDreams\"},{\"l\":\"https://static.tacdn.com/img2/flights/partners/null\",\"n\":\"Kayak\",\"i\":\"Kayak\"},{\"l\":\"https://content.r9cdn.net/rimg/provider-logos/airlines/h/FLIGHTHUBUS.png?crop=false&width=166&height=62&fallback=default3.png&_v=c3600612ab6c56f860f7076a8faa89d0\",\"n\":\"FlightHub\",\"i\":\"FlightHub\"},{\"l\":\"https://content.r9cdn.net/rimg/provider-logos/airlines/h/CHEAPOAIR.png?crop=false&width=166&height=62&fallback=default2.png&_v=5eb11191c415b271717e37aa9870f06f\",\"n\":\"CheapOair\",\"i\":\"CheapOair\"},{\"l\":\"https://static.tacdn.com/img2/flights/partners/highres/blank.png\",\"n\":\"Delta\",\"i\":\"Delta\"},{\"l\":\"https://content.r9cdn.net/rimg/provider-logos/airlines/h/PRICELINEFLIGHTS.png?crop=false&width=166&height=62&fallback=default3.png&_v=b3e81884c7678f954cce1ed2c441273a\",\"n\":\"Priceline\",\"i\":\"Priceline\"},{\"l\":\"https://content.r9cdn.net/rimg/provider-logos/airlines/h/NK.png?crop=false&width=166&height=62&fallback=default1.png&_v=2ec6f00d181725322e6a0f66550c2c23\",\"n\":\"Spirit Airlines\",\"i\":\"Spirit Airlines\"},{\"l\":\"https://content.r9cdn.net/rimg/provider-logos/airlines/h/FLIGHTHUB.us.png?crop=false&width=166&height=62&fallback=default1.png&_v=68e6739b9b9c7ecadd193ad1c25c8869\",\"n\":\"JustFly\",\"i\":\"JustFly\"},{\"l\":\"https://content.r9cdn.net/rimg/provider-logos/airlines/h/AA.png?crop=false&width=166&height=62&fallback=default2.png&_v=8825776e1a4873b3aa558bfa4d430fa3\",\"n\":\"American Airlines\",\"i\":\"American Airlines\"}],\"airports\":[{\"d\":\"Newark, NJ - Newark International Airport (EWR)\",\"c\":\"EWR\",\"i\":46671,\"g\":{\"lat\":40.691387,\"lon\":-74.17472},\"cc\":\"US\",\"cn\":\"Newark\",\"n\":\"Newark Liberty Intl Airport\",\"tz\":\"America/New_York\",\"st\":\"NJ\"},{\"d\":\"Washington, D.C., DC - Ronald Reagan National Airport (DCA)\",\"c\":\"DCA\",\"i\":28970,\"g\":{\"lat\":38.850277,\"lon\":-77.040276},\"cc\":\"US\",\"cn\":\"Washington DC\",\"n\":\"Ronald Reagan National Airport\",\"tz\":\"America/New_York\",\"st\":\"DC\"},{\"d\":\"New York City, NY - LaGuardia Airport (LGA)\",\"c\":\"LGA\",\"i\":60763,\"g\":{\"lat\":40.774166,\"lon\":-73.87278},\"cc\":\"US\",\"cn\":\"New York City\",\"n\":\"La Guardia Airport\",\"tz\":\"America/New_York\",\"st\":\"NY\"},{\"d\":\"Orlando, FL - Orlando International Airport (MCO)\",\"c\":\"MCO\",\"i\":34515,\"g\":{\"lat\":28.416945,\"lon\":-81.30666},\"cc\":\"US\",\"cn\":\"Orlando\",\"n\":\"Orlando Intl Airport\",\"tz\":\"America/New_York\",\"st\":\"FL\"},{\"d\":\"New York City, NY - All Airports (NYC)\",\"c\":\"NYC\",\"i\":60763,\"g\":{\"lat\":40.717777,\"lon\":-74.02889},\"cc\":\"US\",\"cn\":\"New York City\",\"n\":\"New York City Airports\",\"tz\":\"America/New_York\",\"st\":\"NY\"},{\"d\":\"St. Louis, MO - Lambert-St. Louis International Airport (STL)\",\"c\":\"STL\",\"i\":44881,\"g\":{\"lat\":38.749443,\"lon\":-90.36889},\"cc\":\"US\",\"cn\":\"Saint Louis\",\"n\":\"Lambert-St. Louis Intl Airport\",\"tz\":\"America/Chicago\",\"st\":\"MO\"},{\"d\":\"Charlotte, NC - Douglas Airport (CLT)\",\"c\":\"CLT\",\"i\":49022,\"g\":{\"lat\":35.213333,\"lon\":-80.94861},\"cc\":\"US\",\"cn\":\"Charlotte\",\"n\":\"Douglas Airport\",\"tz\":\"America/New_York\",\"st\":\"NC\"},{\"d\":\"Washington, D.C., DC - Dulles International Airport (IAD)\",\"c\":\"IAD\",\"i\":28970,\"g\":{\"lat\":38.953888,\"lon\":-77.45611},\"cc\":\"US\",\"cn\":\"Washington DC\",\"n\":\"Dulles Intl Airport\",\"tz\":\"America/New_York\",\"st\":\"DC\"}],\"air_watch_info\":{\"sw\":false,\"ws\":false},\"disclaimers\":[{\"st\":\"TripAdvisor LLC is not a booking agent and does not charge any service fees to users of our site... (more)\",\"lt\":\"Tripadvisor is not a booking agent and does not charge service fees to users of our site or guarantee the availability of prices advertised on our site. Our partners (airlines and booking agents) who list airfare and travel packages on Tripadvisor are required by law to include all fees and surcharges in their prices. Examples include the Federal Sept. 11th Security Fee, international departure and arrival taxes & fees, federal excise tax, and other service, handling and miscellaneous fees and surcharges. When you book with our partners, please check their site for a full disclosure of all applicable fees as required by the U.S. Department of Transportation. Airfares are generally quoted per person in USD unless otherwise noted.\",\"dt\":\"SEARCH_RESULT\",\"ct\":\"Learn more\"},{\"st\":\"Baggage fees may apply\",\"lt\":\"Prices subject to change. Fares found on Tripadvisor do not include baggage fees. Therefore, additional baggage <u>and other</u> fees may apply.\",\"lk\":\"https://www.tripadvisor.com/AirlineFees\",\"dt\":\"BAGGAGE\",\"ct\":\"Learn more\"}],\"fly_score_info\":{\"TERRIBLE\":\"This flight gets a terrible score based on the quality of the aircraft, amenities, flight duration, and Tripadvisor reviews.\",\"AVERAGE\":\"This flight gets an average score based on the quality of the aircraft, amenities, flight duration, and Tripadvisor reviews.\",\"POOR\":\"This flight gets a poor score based on the quality of the aircraft, amenities, flight duration, and Tripadvisor reviews.\",\"EXCELLENT\":\"This flight gets an excellent score based on the quality of the aircraft, amenities, flight duration, and Tripadvisor reviews.\",\"VERY_GOOD\":\"This flight gets a very good score based on the quality of the aircraft, amenities, flight duration, and Tripadvisor reviews.\"},\"recommended_itins\":{\"BEST_VALUE_3\":{\"key\":\"00UAUA0.1107STL0.1325EWR0\",\"ac\":\"00UAUA0.1107STL0.1325EWR0\",\"l\":[{\"pr\":{\"p\":138.90001,\"f\":0.0,\"dp\":\"$139\",\"df\":\"$0.00\"},\"id\":\"Kayak|2|4\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|UA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"EWR\",\"c\":0,\"m\":\"UA\",\"o\":\"UA\",\"e\":\"Boeing 737-900\",\"f\":\"659\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T06:07:00-05:00\",\"ad\":\"2023-03-24T09:25:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":870.70996}],\"key\":\"00UAUA0.1107STL0.1325EWR0\",\"lo\":[],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"},\"BEST_VALUE_1\":{\"key\":\"00AAAA0.1850STL0.2105LGA0\",\"ac\":\"00AAAA0.1850STL0.2105LGA0\",\"l\":[{\"pr\":{\"p\":138.90001,\"f\":0.0,\"dp\":\"$139\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|36\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|AA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"LGA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Airbus A319\",\"f\":\"1104\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T13:50:00-05:00\",\"ad\":\"2023-03-24T17:05:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":886.8678}],\"key\":\"00AAAA0.1850STL0.2105LGA0\",\"lo\":[],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"},\"BEST_VALUE_2\":{\"key\":\"00AAAA0.1244STL0.1500LGA0\",\"ac\":\"00AAAA0.1244STL0.1500LGA0\",\"l\":[{\"pr\":{\"p\":138.90001,\"f\":0.0,\"dp\":\"$139\",\"df\":\"$0.00\"},\"id\":\"Kayak|1|28\",\"m\":\"KayakUSMeta\",\"s\":\"Kayak|AA\",\"pl\":[]}],\"f\":[{\"l\":[{\"da\":\"STL\",\"aa\":\"LGA\",\"c\":0,\"m\":\"AA\",\"o\":\"AA\",\"e\":\"Airbus A319\",\"f\":\"1825\",\"si\":0,\"n\":0,\"dd\":\"2023-03-24T07:44:00-05:00\",\"ad\":\"2023-03-24T11:00:00-04:00\",\"tt\":\"f\",\"stf\":false,\"ac\":[],\"di\":886.8678}],\"key\":\"00AAAA0.1244STL0.1500LGA0\",\"lo\":[],\"od\":[]}],\"fsl\":\"EXCELLENT\",\"fs\":0.0,\"od\":\"\"}},\"search_params\":{\"pvid\":\"\",\"et\":1675839048,\"so\":\"PRICE\",\"t\":{\"a\":3,\"s\":0,\"c\":[]},\"s\":[{\"dd\":\"2023-03-24\",\"o\":\"STL\",\"d\":\"NYC\",\"no\":false,\"nd\":false}],\"sid\":\"364aa989-6bab-4c0e-bc19-57c0fda1fd45.146\",\"c\":0,\"n\":15,\"it\":\"ONE_WAY\",\"o\":0,\"st\":\"2023-02-08T06:21:48Z\",\"f\":{\"ss\":[],\"mc\":[],\"ns\":[],\"oc\":[],\"da\":[],\"aa\":[],\"ca\":[],\"plp\":[],\"al\":[],\"tt\":[],\"am\":[]}}}");
            //ViewBag.flightResults = GetPollResults(sidFromCreateSession);
            ViewBag.itineraries = (from itinerary in (JArray)ViewBag.flightResults["itineraries"] 
                                   from flights in itinerary["f"]
                                   from layover in flights["l"]
                                   select layover).ToArray();
            ViewBag.carrierImages = (from carrier in (JArray)ViewBag.flightResults["carriers"]
                                     select new { key = carrier["c"].ToString(), value = carrier["l"].ToString() }
                                     ).ToDictionary(x => x.key, x => x.value);
            return View();
        }

        private async Task<JArray> GetPollResults(string sidFromCreateSession)
        {
            var client = new HttpClient();

            HttpRequestMessage request = GetFlightPollHeaderRequest(sidFromCreateSession);
            using var response = await client.SendAsync(request);
            Environment.GetCommandLineArgs();
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            JArray value = JArray.Parse(body);
            //data = value[0];
            return value;
        }

        private JToken GetFlightDetails(SearchTravellerViewModel searchTravellerViewModel)
        {
            string destinationAirLineCode = GetAirPortDetails(searchTravellerViewModel.DesiredDestination).Result;
            Task<string> flightSession = GetFlightDetails(searchTravellerViewModel.CurrentLocation,
                                        destinationAirLineCode,
                                        new DateTime(searchTravellerViewModel.StartDate.Year, searchTravellerViewModel.StartDate.Month, searchTravellerViewModel.StartDate.Day),
                                        searchTravellerViewModel.NoOfTravellers);
            Task<JArray> flightResults = GetPollResults(flightSession.Result.ToString());
            JToken result = JToken.Parse(flightResults.Result.ToString());
            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}