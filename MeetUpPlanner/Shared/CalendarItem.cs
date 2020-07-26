﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;


namespace MeetUpPlanner.Shared
{
    public class CalendarItem : CosmosDBEntity
    {
        [JsonProperty(PropertyName = "title", NullValueHandling = NullValueHandling.Ignore), Display(Name = "Titel", Prompt = "Titel der Veranstaltung"), MaxLength(120, ErrorMessage = "Titel zu lang."), Required(ErrorMessage = "Bitte Titel eingeben.")]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "startDate"), Display(Name = "Start", Prompt = "Bitte Startdatum und -zeit angeben."), Required(ErrorMessage = "Bitte den Beginn der Veranstaltung angeben.")]
        public DateTime StartDate { get; set; }
        [JsonProperty(PropertyName = "place"), Display(Name = "Ort", Prompt = "Wo findet die Veranstaltung statt bzw. wo ist der Start"), MaxLength(256), Required(ErrorMessage = "Bitte den Startort angeben.")]
        public string Place { get; set; }
        [JsonProperty(PropertyName = "hostFirstName", NullValueHandling = NullValueHandling.Ignore), MaxLength(100), Required(ErrorMessage = "Gastgeber bitte eingeben.")]
        public string HostFirstName { get; set; }
        [JsonProperty(PropertyName = "hostLastName", NullValueHandling = NullValueHandling.Ignore), MaxLength(100), Required(ErrorMessage = "Gastgeber bitte eingeben.")]
        public string HostLastName { get; set; }
        [JsonProperty(PropertyName = "summary", NullValueHandling = NullValueHandling.Ignore), Display(Name = "Zusammenfassung", Prompt = "Kurze Zusammenfassung des Termins"), MaxLength(5000, ErrorMessage = "Zusammenfassung zu lang.")]
        public string Summary { get; set; }
        [JsonProperty(PropertyName = "maxRegistrationsCount", NullValueHandling = NullValueHandling.Ignore), Range(2.0, 50.0, ErrorMessage = "Gruppengröße nicht im gültigen Bereich."), Display(Name = "Maximale Anzahl Teilnehmer", Prompt = "Anzahl eingeben"), Required(ErrorMessage = "Max. Anzahl Teilnehmer eingeben")]
        public int MaxRegistrationsCount { get; set; } = 10;
        [JsonProperty(PropertyName = "privateKeyword", NullValueHandling = NullValueHandling.Ignore), MaxLength(50, ErrorMessage = "Privates Schlüsselwort zu lang.")]
        public string PrivateKeyword { get; set; }
        [JsonProperty(PropertyName = "levelDescription")]
        public string LevelDescription { get; set; }
        [JsonProperty(PropertyName = "tempo"), Required(ErrorMessage = "Bitte geplante Geschwindigkeit angeben."), MaxLength(40, ErrorMessage = "Tempo-Angabe zu lang.")]
        public string Tempo { get; set; }
        [JsonProperty(PropertyName = "link", NullValueHandling = NullValueHandling.Ignore), MaxLength(120, ErrorMessage = "Link zu lang"), UIHint("url") ]
        public string Link { get; set; }

        /// <summary>
        /// Constructor with some suggestions about start-time: Schedule for the next day. On Saturdays and Sundays 
        /// StartTime is 10 am otherwise 18 pm
        /// </summary>
        public CalendarItem()
        {
            DateTime startDate = DateTime.Today.AddDays(1.0); // Default is tomorrow
            if ( startDate.DayOfWeek == DayOfWeek.Saturday || startDate.DayOfWeek == DayOfWeek.Sunday)
            {
                // On Saturday and Sunday starttime is 10 am
                this.StartDate = DateTime.Today.AddDays(1.0).AddHours(10.0);
            }
            else
            {
                this.StartDate = DateTime.Today.AddDays(1.0).AddHours(18.0);
            }
        }
        [JsonIgnore]
        public string HostDisplayName
        {
            get
            {
                return HostFirstName + " " + HostLastName[0] + ".";
            }
        }
        /// <summary>
        /// Returns a string ready to display in (German) UI.
        /// </summary>
        /// <returns></returns>
        public string GetStartDateAsString()
        {
            string[] weekdays = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };
            string dateString = String.Empty;
            dateString = weekdays[(int)StartDate.DayOfWeek] + ", " + this.StartDate.ToString("dd.MM.yyy HH:mm") + " Uhr";
            return dateString;
        }
    }
}