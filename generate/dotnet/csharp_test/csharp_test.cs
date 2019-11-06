﻿using System;
using System.IO;
using System.Text.RegularExpressions;

using CosineKitty;

namespace csharp_test
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("csharp_test: starting");
                if (TestTime() != 0) return 1;
                if (MoonTest() != 0) return 1;
                if (RiseSetTest("../../riseset/riseset.txt") != 0) return 1;
                if (SeasonsTest("../../seasons/seasons.txt") != 0) return 1;
                if (MoonPhaseTest("../../moonphase/moonphases.txt") != 0) return 1;
                if (ElongationTest() != 0) return 1;
                if (AstroCheck() != 0) return 1;
                Console.WriteLine("csharp_test: PASS");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("charp_test: EXCEPTION: {0}", ex);
                return 1;
            }
        }

        static int TestTime()
        {
            const int year = 2018;
            const int month = 12;
            const int day = 2;
            const int hour = 18;
            const int minute = 30;
            const int second = 12;
            const int milli = 543;

            DateTime d = new DateTime(year, month, day, hour, minute, second, milli, DateTimeKind.Utc);
            AstroTime time = new AstroTime(d);
            Console.WriteLine("TestTime: text={0}, ut={1}, tt={2}", time.ToString(), time.ut.ToString("F6"), time.tt.ToString("F6"));

            const double expected_ut = 6910.270978506945;
            double diff = time.ut - expected_ut;
            if (Math.Abs(diff) > 1.0e-12)
            {
                Console.WriteLine("TestTime: ERROR - excessive UT error {0}", diff);
                return 1;
            }

            const double expected_tt = 6910.271779431480;
            diff = time.tt - expected_tt;
            if (Math.Abs(diff) > 1.0e-12)
            {
                Console.WriteLine("TestTime: ERROR - excessive TT error {0}", diff);
                return 1;
            }

            DateTime utc = time.ToUtcDateTime();
            if (utc.Year != year || utc.Month != month || utc.Day != day || utc.Hour != hour || utc.Minute != minute || utc.Second != second || utc.Millisecond != milli)
            {
                Console.WriteLine("TestTime: ERROR - Expected {0:o}, found {1:o}", d, utc);
                return 1;
            }

            return 0;
        }

        static int MoonTest()
        {
            var time = new AstroTime(2019, 6, 24, 15, 45, 37);
            AstroVector vec = Astronomy.GeoVector(Body.Moon, time, Aberration.None);
            Console.WriteLine("MoonTest: {0} {1} {2}", vec.x.ToString("f17"), vec.y.ToString("f17"), vec.z.ToString("f17"));

            double dx = vec.x - (+0.002674036155459549);
            double dy = vec.y - (-0.0001531716308218381);
            double dz = vec.z - (-0.0003150201604895409);
            double diff = Math.Sqrt(dx*dx + dy*dy + dz*dz);
            Console.WriteLine("MoonTest: diff = {0}", diff.ToString("g5"));
            if (diff > 4.34e-19)
            {
                Console.WriteLine("MoonTest: EXCESSIVE ERROR");
                return 1;
            }

            return 0;
        }

        static int AstroCheck()
        {
            const string filename = "csharp_check.txt";
            using (StreamWriter outfile = File.CreateText(filename))
            {
                var bodylist = new Body[]
                {
                    Body.Sun, Body.Mercury, Body.Venus, Body.Earth, Body.Mars,
                    Body.Jupiter, Body.Saturn, Body.Uranus, Body.Neptune, Body.Pluto
                };

                var observer = new Observer(29.0, -81.0, 10.0);
                var time = new AstroTime(new DateTime(1700, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                var stop = new AstroTime(new DateTime(2200, 1, 1, 0, 0, 0, DateTimeKind.Utc));

                AstroVector pos;
                Equatorial j2000, ofdate;
                Topocentric hor;

                outfile.WriteLine("o {0} {1} {2}", observer.latitude, observer.longitude, observer.height);
                while (time.tt < stop.tt)
                {
                    foreach (Body body in bodylist)
                    {
                        pos = Astronomy.HelioVector(body, time);
                        outfile.WriteLine("v {0} {1} {2} {3} {4}", body, pos.t.tt.ToString("G17"), pos.x.ToString("G17"), pos.y.ToString("G17"), pos.z.ToString("G17"));
                        if (body != Body.Earth)
                        {
                            j2000 = Astronomy.Equator(body, time, observer, EquatorEpoch.J2000, Aberration.None);
                            ofdate = Astronomy.Equator(body, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                            hor = Astronomy.Horizon(time, observer, ofdate.ra, ofdate.dec, Refraction.None);
                            outfile.WriteLine("s {0} {1} {2} {3} {4} {5} {6} {7}",
                                body,
                                time.tt.ToString("G17"), time.ut.ToString("G17"),
                                j2000.ra.ToString("G17"), j2000.dec.ToString("G17"), j2000.dist.ToString("G17"),
                                hor.azimuth.ToString("G17"), hor.altitude.ToString("G17"));
                        }
                    }

                    pos = Astronomy.GeoVector(Body.Moon, time, Aberration.None);
                    outfile.WriteLine("v GM {0} {1} {2} {3}", pos.t.tt.ToString("G17"), pos.x.ToString("G17"), pos.y.ToString("G17"), pos.z.ToString("G17"));
                    j2000 = Astronomy.Equator(Body.Moon, time, observer, EquatorEpoch.J2000, Aberration.None);
                    ofdate = Astronomy.Equator(Body.Moon, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                    hor = Astronomy.Horizon(time, observer, ofdate.ra, ofdate.dec, Refraction.None);
                    outfile.WriteLine("s GM {0} {1} {2} {3} {4} {5} {6}",
                        time.tt.ToString("G17"), time.ut.ToString("G17"),
                        j2000.ra.ToString("G17"), j2000.dec.ToString("G17"), j2000.dist.ToString("G17"),
                        hor.azimuth.ToString("G17"), hor.altitude.ToString("G17"));

                    time = time.AddDays(10.0 + Math.PI/100.0);
                }
            }
            Console.WriteLine("AstroCheck: finished");
            return 0;
        }

        static int SeasonsTest(string filename)
        {
            var re = new Regex(@"^(\d+)-(\d+)-(\d+)T(\d+):(\d+)Z\s+([A-Za-z]+)\s*$");
            using (StreamReader infile = File.OpenText(filename))
            {
                string line;
                int lnum = 0;
                int current_year = 0;
                int mar_count=0, jun_count=0, sep_count=0, dec_count=0;
                double max_minutes = 0.0;
                SeasonsInfo seasons = new SeasonsInfo();
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    /*
                        2019-01-03T05:20Z Perihelion
                        2019-03-20T21:58Z Equinox
                        2019-06-21T15:54Z Solstice
                        2019-07-04T22:11Z Aphelion
                        2019-09-23T07:50Z Equinox
                        2019-12-22T04:19Z Solstice
                    */
                    Match m = re.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("SeasonsTest: ERROR {0} line {1}: cannot parse", filename, lnum);
                        return 1;
                    }

                    int year = int.Parse(m.Groups[1].Value);
                    int month = int.Parse(m.Groups[2].Value);
                    int day = int.Parse(m.Groups[3].Value);
                    int hour = int.Parse(m.Groups[4].Value);
                    int minute = int.Parse(m.Groups[5].Value);
                    string name = m.Groups[6].Value;
                    var correct_time = new AstroTime(year, month, day, hour, minute, 0);

                    if (year != current_year)
                    {
                        current_year = year;
                        seasons = Astronomy.Seasons(year);
                    }

                    AstroTime calc_time = null;
                    if (name == "Equinox")
                    {
                        switch (month)
                        {
                            case 3:
                                calc_time = seasons.mar_equinox;
                                ++mar_count;
                                break;

                            case 9:
                                calc_time = seasons.sep_equinox;
                                ++sep_count;
                                break;

                            default:
                                Console.WriteLine("SeasonsTest: {0} line {1}: Invalid equinox date in test data.", filename, lnum);
                                return 1;
                        }
                    }
                    else if (name == "Solstice")
                    {
                        switch (month)
                        {
                            case 6:
                                calc_time = seasons.jun_solstice;
                                ++jun_count;
                                break;

                            case 12:
                                calc_time = seasons.dec_solstice;
                                ++dec_count;
                                break;

                            default:
                                Console.WriteLine("SeasonsTest: {0} line {1}: Invalid solstice date in test data.", filename, lnum);
                                return 1;
                        }
                    }
                    else if (name == "Aphelion")
                    {
                        /* not yet calculated */
                        continue;
                    }
                    else if (name == "Perihelion")
                    {
                        /* not yet calculated */
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("SeasonsTest: {0} line {1}: unknown event type {2}", filename, lnum, name);
                        return 1;
                    }

                    /* Verify that the calculated time matches the correct time for this event. */
                    double diff_minutes = (24.0 * 60.0) * Math.Abs(calc_time.tt - correct_time.tt);
                    if (diff_minutes > max_minutes)
                        max_minutes = diff_minutes;

                    if (diff_minutes > 1.7)
                    {
                        Console.WriteLine("SeasonsTest: %s line %d: excessive error (%s): %lf minutes.\n", filename, lnum, name, diff_minutes);
                        return 1;
                    }
                }
                Console.WriteLine("SeasonsTest: verified {0} lines from file {1} : max error minutes = {2:0.000}", lnum, filename, max_minutes);
                Console.WriteLine("SeasonsTest: Event counts: mar={0}, jun={1}, sep={2}, dec={3}", mar_count, jun_count, sep_count, dec_count);
                return 0;
            }
        }

        static int MoonPhaseTest(string filename)
        {
            using (StreamReader infile = File.OpenText(filename))
            {
                const double threshold_seconds = 120.0;
                int lnum = 0;
                string line;
                double max_arcmin = 0.0;
                int prev_year = 0;
                int expected_quarter = 0;
                int quarter_count = 0;
                double maxdiff = 0.0;
                MoonQuarterInfo mq = new MoonQuarterInfo();
                var re = new Regex(@"^([0-3])\s+(\d+)-(\d+)-(\d+)T(\d+):(\d+):(\d+)\.000Z$");
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    /*
                        0 1800-01-25T03:21:00.000Z
                        1 1800-02-01T20:40:00.000Z
                        2 1800-02-09T17:26:00.000Z
                        3 1800-02-16T15:49:00.000Z
                    */
                    Match m = re.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("MoonPhaseTest: ERROR {0} line {1}: cannot parse", filename, lnum);
                        return 1;
                    }
                    int quarter = int.Parse(m.Groups[1].Value);
                    int year = int.Parse(m.Groups[2].Value);
                    int month = int.Parse(m.Groups[3].Value);
                    int day = int.Parse(m.Groups[4].Value);
                    int hour = int.Parse(m.Groups[5].Value);
                    int minute = int.Parse(m.Groups[6].Value);
                    int second = int.Parse(m.Groups[7].Value);

                    double expected_elong = 90.0 * quarter;
                    AstroTime expected_time = new AstroTime(year, month, day, hour, minute, second);
                    double calc_elong = Astronomy.MoonPhase(expected_time);
                    double degree_error = Math.Abs(calc_elong - expected_elong);
                    if (degree_error > 180.0)
                        degree_error = 360.0 - degree_error;
                    double arcmin = 60.0 * degree_error;
                    if (arcmin > 1.0)
                    {
                        Console.WriteLine("MoonPhaseTest({0} line {1}): EXCESSIVE ANGULAR ERROR: {2} arcmin", filename, lnum, arcmin);
                        return 1;
                    }
                    if (arcmin > max_arcmin)
                        max_arcmin = arcmin;

                    if (year != prev_year)
                    {
                        prev_year = year;
                        /* The test data contains a single year's worth of data for every 10 years. */
                        /* Every time we see the year value change, it breaks continuity of the phases. */
                        /* Start the search over again. */
                        AstroTime start_time = new AstroTime(year, 1, 1, 0, 0, 0);
                        mq = Astronomy.SearchMoonQuarter(start_time);
                        expected_quarter = -1;  /* we have no idea what the quarter should be */
                    }
                    else
                    {
                        /* Yet another lunar quarter in the same year. */
                        expected_quarter = (1 + mq.quarter) % 4;
                        mq = Astronomy.NextMoonQuarter(mq);

                        /* Make sure we find the next expected quarter. */
                        if (expected_quarter != mq.quarter)
                        {
                            Console.WriteLine("MoonPhaseTest({0} line {1}): SearchMoonQuarter returned quarter {2}, but expected {3}", filename, lnum, mq.quarter, expected_quarter);
                            return 1;
                        }
                    }
                    ++quarter_count;
                    /* Make sure the time matches what we expect. */
                    double diff_seconds = Math.Abs(mq.time.tt - expected_time.tt) * (24.0 * 3600.0);
                    if (diff_seconds > threshold_seconds)
                    {
                        Console.WriteLine("MoonPhaseTest({0} line {1}): excessive time error {2:0.000} seconds", filename, lnum, diff_seconds);
                        return 1;
                    }

                    if (diff_seconds > maxdiff)
                        maxdiff = diff_seconds;
                }

                Console.WriteLine("MoonPhaseTest: passed {0} lines for file {1} : max_arcmin = {2:0.000000}, maxdiff = {3:0.000} seconds, {4} quarters",
                    lnum, filename, max_arcmin, maxdiff, quarter_count);

                return 0;
            }
        }

        static int RiseSetTest(string filename)
        {
            using (StreamReader infile = File.OpenText(filename))
            {
                int lnum = 0;
                string line;
                var re = new Regex(@"^([A-Za-z]+)\s+([\-\+]?\d+\.?\d*)\s+([\-\+]?\d+\.?\d*)\s+(\d+)-(\d+)-(\d+)T(\d+):(\d+)Z\s+([rs])\s*$");
                Body current_body = Body.Invalid;
                Observer observer = null;
                AstroTime r_search_date = null, s_search_date = null;
                AstroTime r_evt = null, s_evt = null;     /* rise event, set event: search results */
                AstroTime a_evt = null, b_evt = null;     /* chronologically first and second events */
                Direction a_dir = Direction.Rise, b_dir = Direction.Rise;
                const double nudge_days = 0.01;
                double sum_minutes = 0.0;
                double max_minutes = 0.0;

                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;

                    // Moon  103 -61 1944-01-02T17:08Z s
                    // Moon  103 -61 1944-01-03T05:47Z r
                    Match m = re.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("RiseSetTest({0} line {1}): invalid input format", filename, lnum);
                        return 1;
                    }
                    Body body = Enum.Parse<Body>(m.Groups[1].Value);
                    double longitude = double.Parse(m.Groups[2].Value);
                    double latitude = double.Parse(m.Groups[3].Value);
                    int year = int.Parse(m.Groups[4].Value);
                    int month = int.Parse(m.Groups[5].Value);
                    int day = int.Parse(m.Groups[6].Value);
                    int hour = int.Parse(m.Groups[7].Value);
                    int minute = int.Parse(m.Groups[8].Value);
                    Direction direction = (m.Groups[9].Value == "r") ? Direction.Rise : Direction.Set;
                    var correct_date = new AstroTime(year, month, day, hour, minute, 0);

                    /* Every time we see a new geographic location or body, start a new iteration */
                    /* of finding all rise/set times for that UTC calendar year. */
                    if (observer == null || observer.latitude != latitude || observer.longitude != longitude || current_body != body)
                    {
                        current_body = body;
                        observer = new Observer(latitude, longitude, 0.0);
                        r_search_date = s_search_date = new AstroTime(year, 1, 1, 0, 0, 0);
                        b_evt = null;
                        Console.WriteLine("RiseSetTest: {0} lat={1} lon={2}", body, latitude, longitude);
                    }

                    if (b_evt != null)
                    {
                        a_evt = b_evt;
                        a_dir = b_dir;
                        b_evt = null;
                    }
                    else
                    {
                        r_evt = Astronomy.SearchRiseSet(body, observer, Direction.Rise, r_search_date, 366.0);
                        if (r_evt == null)
                        {
                            Console.WriteLine("RiseSetTest({0} line {1}): Did not find {2} rise event.", filename, lnum, body);
                            return 1;
                        }

                        s_evt = Astronomy.SearchRiseSet(body, observer, Direction.Set, s_search_date, 366.0);
                        if (s_evt == null)
                        {
                            Console.WriteLine("RiseSetTest({0} line {1}): Did not find {2} rise event.", filename, lnum, body);
                            return 1;
                        }

                        /* Expect the current event to match the earlier of the found dates. */
                        if (r_evt.tt < s_evt.tt)
                        {
                            a_evt = r_evt;
                            b_evt = s_evt;
                            a_dir = Direction.Rise;
                            b_dir = Direction.Set;
                        }
                        else
                        {
                            a_evt = s_evt;
                            b_evt = r_evt;
                            a_dir = Direction.Set;
                            b_dir = Direction.Rise;
                        }

                        /* Nudge the event times forward a tiny amount. */
                        r_search_date = r_evt.AddDays(nudge_days);
                        s_search_date = s_evt.AddDays(nudge_days);
                    }

                    if (a_dir != direction)
                    {
                        Console.WriteLine("RiseSetTest({0} line {1}): expected dir={2} but found {3}", filename, lnum, a_dir, direction);
                        return 1;
                    }
                    double error_minutes = (24.0 * 60.0) * Math.Abs(a_evt.tt - correct_date.tt);
                    sum_minutes += error_minutes * error_minutes;
                    if (error_minutes > max_minutes)
                        max_minutes = error_minutes;

                    if (error_minutes > 0.56)
                    {
                        Console.WriteLine("RiseSetTest({0} line {1}): excessive prediction time error = {2} minutes.\n", filename, lnum, error_minutes);
                        return 1;
                    }
                }

                double rms_minutes = Math.Sqrt(sum_minutes / lnum);
                Console.WriteLine("RiseSetTest: passed {0} lines: time errors in minutes: rms={1}, max={2}", lnum, rms_minutes, max_minutes);
                return 0;
            }
        }

        static int TestElongFile(string filename, double targetRelLon)
        {
            using (StreamReader infile = File.OpenText(filename))
            {
                int lnum = 0;
                string line;
                var re = new Regex(@"^(\d+)-(\d+)-(\d+)T(\d+):(\d+)Z\s+([A-Z][a-z]+)\s*$");
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    /* 2018-05-09T00:28Z Jupiter */
                    Match m = re.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("C# TestElongFile({0} line {1}): invalid data format.", filename, lnum);
                        return 1;
                    }
                    int year = int.Parse(m.Groups[1].Value);
                    int month = int.Parse(m.Groups[2].Value);
                    int day = int.Parse(m.Groups[3].Value);
                    int hour = int.Parse(m.Groups[4].Value);
                    int minute = int.Parse(m.Groups[5].Value);
                    Body body = Enum.Parse<Body>(m.Groups[6].Value);
                    var search_date = new AstroTime(year, 1, 1, 0, 0, 0);
                    var expected_time = new AstroTime(year, month, day, hour, minute, 0);
                    AstroTime search_result = Astronomy.SearchRelativeLongitude(body, targetRelLon, search_date);
                    if (search_result == null)
                    {
                        Console.WriteLine("C# TestElongFile({0} line {1}): SearchRelativeLongitude returned null.", filename, lnum);
                        return 1;
                    }
                    double diff_minutes = (24.0 * 60.0) * (search_result.tt - expected_time.tt);
                    Console.WriteLine("{0} error = {1} minutes.", body, diff_minutes.ToString("f3"));
                    if (Math.Abs(diff_minutes) > 15.0)
                    {
                        Console.WriteLine("C# TestElongFile({0} line {1}): EXCESSIVE ERROR.", filename, lnum);
                        return 1;
                    }
                }
                Console.WriteLine("C# TestElongFile: passed {0} rows of data.", lnum);
                return 0;
            }
        }

        static int TestPlanetLongitudes(Body body, string outFileName, string zeroLonEventName)
        {
            const int startYear = 1700;
            const int stopYear = 2200;
            int count = 0;
            double rlon = 0.0;
            double min_diff = 1.0e+99;
            double max_diff = 1.0e+99;
            double sum_diff = 0.0;

            using (StreamWriter outfile = File.CreateText(outFileName))
            {
                var time = new AstroTime(startYear, 1, 1, 0, 0, 0);
                var stopTime = new AstroTime(stopYear, 1, 1, 0, 0, 0);
                while (time.tt < stopTime.tt)
                {
                    ++count;
                    string event_name = (rlon == 0.0) ? zeroLonEventName : "sup";
                    AstroTime search_result = Astronomy.SearchRelativeLongitude(body, rlon, time);
                    if (search_result == null)
                    {
                        Console.WriteLine("C# TestPlanetLongitudes({0}): SearchRelativeLongitude returned null.", body);
                        return 1;
                    }

                    if (count >= 2)
                    {
                        /* Check for consistent intervals. */
                        /* Mainly I don't want to skip over an event! */
                        double day_diff = search_result.tt - time.tt;
                        sum_diff += day_diff;
                        if (count == 2)
                        {
                            min_diff = max_diff = day_diff;
                        }
                        else
                        {
                            if (day_diff < min_diff)
                                min_diff = day_diff;

                            if (day_diff > max_diff)
                                max_diff = day_diff;
                        }
                    }

                    AstroVector geo = Astronomy.GeoVector(body, search_result, Aberration.Corrected);
                    double dist = geo.Length();
                    outfile.WriteLine("e {0} {1} {2} {3}", body, event_name, search_result.tt.ToString("g17"), dist.ToString("g17"));

                    /* Search for the opposite longitude event next time. */
                    time = search_result;
                    rlon = 180.0 - rlon;
                }
            }

            double thresh;
            switch (body)
            {
                case Body.Mercury:  thresh = 1.65;  break;
                case Body.Mars:     thresh = 1.30;  break;
                default:            thresh = 1.07;  break;
            }

            double ratio = max_diff / min_diff;
            Console.WriteLine("TestPlanetLongitudes({0,7}): {1,5} events, ratio={2,5}, file: {3}", body, count, ratio.ToString("f3"), outFileName);

            if (ratio > thresh)
            {
                Console.WriteLine("TestPlanetLongitudes({0}): excessive event interval ratio.\n", body);
                return 1;
            }
            return 0;
        }

        static int ElongationTest()
        {
            if (0 != TestElongFile("../../longitude/opposition_2018.txt", 0.0)) return 1;
            if (0 != TestPlanetLongitudes(Body.Mercury, "csharp_longitude_Mercury.txt", "inf")) return 1;
            if (0 != TestPlanetLongitudes(Body.Venus,   "csharp_longitude_Venus.txt",   "inf")) return 1;
            if (0 != TestPlanetLongitudes(Body.Mars,    "csharp_longitude_Mars.txt",    "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Jupiter, "csharp_longitude_Jupiter.txt", "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Saturn,  "csharp_longitude_Saturn.txt",  "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Uranus,  "csharp_longitude_Uranus.txt",  "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Neptune, "csharp_longitude_Neptune.txt", "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Pluto,   "csharp_longitude_Pluto.txt",   "opp")) return 1;

            foreach (elong_test_t et in ElongTestData)
                if (0 != TestMaxElong(et))
                    return 1;

            return 0;
        }

        static readonly Regex regexDate = new Regex(@"^(\d+)-(\d+)-(\d+)T(\d+):(\d+)Z$");

        static AstroTime ParseDate(string text)
        {
            Match m = regexDate.Match(text);
            if (!m.Success)
                throw new Exception(string.Format("ParseDate failed for string: '{0}'", text));
            int year = int.Parse(m.Groups[1].Value);
            int month = int.Parse(m.Groups[2].Value);
            int day = int.Parse(m.Groups[3].Value);
            int hour = int.Parse(m.Groups[4].Value);
            int minute = int.Parse(m.Groups[5].Value);
            return new AstroTime(year, month, day, hour, minute, 0);
        }

        static int TestMaxElong(elong_test_t test)
        {
            AstroTime searchTime = ParseDate(test.searchDate);
            AstroTime eventTime = ParseDate(test.eventDate);
            ElongationInfo evt = Astronomy.SearchMaxElongation(test.body, searchTime);
            double hour_diff = 24.0 * Math.Abs(evt.time.tt - eventTime.tt);
            double arcmin_diff = 60.0 * Math.Abs(evt.elongation - test.angle);
            Console.WriteLine("C# TestMaxElong: {0,7} {1,7} elong={2,5} ({3} arcmin, {4} hours)", test.body, test.visibility, evt.elongation, arcmin_diff, hour_diff);
            if (hour_diff > 0.6)
            {
                Console.WriteLine("C# TestMaxElong({0} {1}): excessive hour error.", test.body, test.searchDate);
                return 1;
            }

            if (arcmin_diff > 3.4)
            {
                Console.WriteLine("C# TestMaxElong({0} {1}): excessive arcmin error.", test.body, test.searchDate);
                return 1;
            }

            return 0;
        }

        struct elong_test_t
        {
            public Body body;
            public string searchDate;
            public string eventDate;
            public double angle;
            public Visibility visibility;

            public elong_test_t(Body body, string searchDate, string eventDate, double angle, Visibility visibility)
            {
                this.body = body;
                this.searchDate = searchDate;
                this.eventDate = eventDate;
                this.angle = angle;
                this.visibility = visibility;
            }
        }

        static readonly elong_test_t[] ElongTestData = new elong_test_t[]
        {
            /* Max elongation data obtained from: */
            /* http://www.skycaramba.com/greatest_elongations.shtml */
            new elong_test_t( Body.Mercury, "2010-01-17T05:22Z", "2010-01-27T05:22Z", 24.80, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2010-05-16T02:15Z", "2010-05-26T02:15Z", 25.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2010-09-09T17:24Z", "2010-09-19T17:24Z", 17.90, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2010-12-30T14:33Z", "2011-01-09T14:33Z", 23.30, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2011-04-27T19:03Z", "2011-05-07T19:03Z", 26.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2011-08-24T05:52Z", "2011-09-03T05:52Z", 18.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2011-12-13T02:56Z", "2011-12-23T02:56Z", 21.80, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2012-04-08T17:22Z", "2012-04-18T17:22Z", 27.50, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2012-08-06T12:04Z", "2012-08-16T12:04Z", 18.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2012-11-24T22:55Z", "2012-12-04T22:55Z", 20.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2013-03-21T22:02Z", "2013-03-31T22:02Z", 27.80, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2013-07-20T08:51Z", "2013-07-30T08:51Z", 19.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2013-11-08T02:28Z", "2013-11-18T02:28Z", 19.50, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2014-03-04T06:38Z", "2014-03-14T06:38Z", 27.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2014-07-02T18:22Z", "2014-07-12T18:22Z", 20.90, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2014-10-22T12:36Z", "2014-11-01T12:36Z", 18.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2015-02-14T16:20Z", "2015-02-24T16:20Z", 26.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2015-06-14T17:10Z", "2015-06-24T17:10Z", 22.50, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2015-10-06T03:20Z", "2015-10-16T03:20Z", 18.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2016-01-28T01:22Z", "2016-02-07T01:22Z", 25.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2016-05-26T08:45Z", "2016-06-05T08:45Z", 24.20, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2016-09-18T19:27Z", "2016-09-28T19:27Z", 17.90, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2017-01-09T09:42Z", "2017-01-19T09:42Z", 24.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2017-05-07T23:19Z", "2017-05-17T23:19Z", 25.80, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2017-09-02T10:14Z", "2017-09-12T10:14Z", 17.90, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2017-12-22T19:48Z", "2018-01-01T19:48Z", 22.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2018-04-19T18:17Z", "2018-04-29T18:17Z", 27.00, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2018-08-16T20:35Z", "2018-08-26T20:35Z", 18.30, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2018-12-05T11:34Z", "2018-12-15T11:34Z", 21.30, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2019-04-01T19:40Z", "2019-04-11T19:40Z", 27.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2019-07-30T23:08Z", "2019-08-09T23:08Z", 19.00, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2019-11-18T10:31Z", "2019-11-28T10:31Z", 20.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2010-03-29T23:32Z", "2010-04-08T23:32Z", 19.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2010-07-28T01:03Z", "2010-08-07T01:03Z", 27.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2010-11-21T15:42Z", "2010-12-01T15:42Z", 21.50, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2011-03-13T01:07Z", "2011-03-23T01:07Z", 18.60, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2011-07-10T04:56Z", "2011-07-20T04:56Z", 26.80, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2011-11-04T08:40Z", "2011-11-14T08:40Z", 22.70, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2012-02-24T09:39Z", "2012-03-05T09:39Z", 18.20, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2012-06-21T02:00Z", "2012-07-01T02:00Z", 25.70, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2012-10-16T21:59Z", "2012-10-26T21:59Z", 24.10, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2013-02-06T21:24Z", "2013-02-16T21:24Z", 18.10, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2013-06-02T16:45Z", "2013-06-12T16:45Z", 24.30, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2013-09-29T09:59Z", "2013-10-09T09:59Z", 25.30, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2014-01-21T10:00Z", "2014-01-31T10:00Z", 18.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2014-05-15T07:06Z", "2014-05-25T07:06Z", 22.70, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2014-09-11T22:20Z", "2014-09-21T22:20Z", 26.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2015-01-04T20:26Z", "2015-01-14T20:26Z", 18.90, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2015-04-27T04:46Z", "2015-05-07T04:46Z", 21.20, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2015-08-25T10:20Z", "2015-09-04T10:20Z", 27.10, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2015-12-19T03:11Z", "2015-12-29T03:11Z", 19.70, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2016-04-08T14:00Z", "2016-04-18T14:00Z", 19.90, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2016-08-06T21:24Z", "2016-08-16T21:24Z", 27.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2016-12-01T04:36Z", "2016-12-11T04:36Z", 20.80, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2017-03-22T10:24Z", "2017-04-01T10:24Z", 19.00, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2017-07-20T04:34Z", "2017-07-30T04:34Z", 27.20, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2017-11-14T00:32Z", "2017-11-24T00:32Z", 22.00, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2018-03-05T15:07Z", "2018-03-15T15:07Z", 18.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2018-07-02T05:24Z", "2018-07-12T05:24Z", 26.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2018-10-27T15:25Z", "2018-11-06T15:25Z", 23.30, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2019-02-17T01:23Z", "2019-02-27T01:23Z", 18.10, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2019-06-13T23:14Z", "2019-06-23T23:14Z", 25.20, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2019-10-10T04:00Z", "2019-10-20T04:00Z", 24.60, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2010-12-29T15:57Z", "2011-01-08T15:57Z", 47.00, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2012-08-05T08:59Z", "2012-08-15T08:59Z", 45.80, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2014-03-12T19:25Z", "2014-03-22T19:25Z", 46.60, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2015-10-16T06:57Z", "2015-10-26T06:57Z", 46.40, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2017-05-24T13:09Z", "2017-06-03T13:09Z", 45.90, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2018-12-27T04:24Z", "2019-01-06T04:24Z", 47.00, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2010-08-10T03:19Z", "2010-08-20T03:19Z", 46.00, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2012-03-17T08:03Z", "2012-03-27T08:03Z", 46.00, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2013-10-22T08:00Z", "2013-11-01T08:00Z", 47.10, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2015-05-27T18:46Z", "2015-06-06T18:46Z", 45.40, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2017-01-02T13:19Z", "2017-01-12T13:19Z", 47.10, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2018-08-07T17:02Z", "2018-08-17T17:02Z", 45.90, Visibility.Evening )
        };
    }
}
