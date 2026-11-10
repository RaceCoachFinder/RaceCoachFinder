(function () {
    'use strict';

    const TAAL_KEY = 'kcf_taal';
    const originals = new WeakMap(); // text node → original Dutch string

    // ============================================================
    //  VERTAALWOORDENBOEK  Nederlands → Engels
    // ============================================================
    const EN = {

        // ===== NAVIGATIE / NAV =====
        'Home': 'Home',
        'Coaches': 'Coaches',
        'Groepscoaching': 'Group Coaching',
        'Persoonlijk coachen': 'Personal Coaching',
        'Berichten': 'Messages',
        'Inloggen': 'Log in',
        'Registreren': 'Register',
        'Uitloggen': 'Log out',
        'Instellingen': 'Settings',
        'Wissel naar rijder': 'Switch to rider',
        'Wissel naar coach': 'Switch to coach',
        'Actief als coach': 'Active as coach',
        'Actief als rijder': 'Active as rider',
        'Beheerder': 'Admin',

        // ===== FOOTER =====
        'Hoe werkt het': 'How it works',
        'Tips': 'Tips',
        'Contact': 'Contact',
        'Privacybeleid': 'Privacy Policy',
        'Voorwaarden': 'Terms',
        'Disclaimer': 'Disclaimer',
        '© 2026 RaceCoachFinder': '© 2026 RaceCoachFinder',

        // ===== ALGEMENE KNOPPEN / STATUS =====
        'Annuleren': 'Cancel',
        'Opslaan': 'Save',
        'Bewerken': 'Edit',
        'Verwijderen': 'Delete',
        'Sluiten': 'Close',
        'Verstuur': 'Send',
        'Zoeken': 'Search',
        'Reset': 'Reset',
        'Alle': 'All',
        'Bezig...': 'Processing…',
        'Laden...': 'Loading…',
        'Opgeslagen!': 'Saved!',
        'Verwijderd!': 'Deleted!',
        'Actief': 'Active',
        'Verlopen': 'Expired',
        'Verwijderd': 'Deleted',
        'Gratis': 'Free',
        'Beschikbaar': 'Available',
        'Volgeboekt': 'Fully booked',
        'Ja': 'Yes',
        'Nee': 'No',

        // ===== GEMEENSCHAPPELIJKE FORMULIER-LABELS =====
        'Naam': 'Name',
        'Voornaam': 'First name',
        'Achternaam': 'Last name',
        'E-mailadres': 'Email address',
        'Wachtwoord': 'Password',
        'Bevestig wachtwoord': 'Confirm password',
        'Huidig wachtwoord': 'Current password',
        'Nieuw wachtwoord': 'New password',
        'Locatie': 'Location',
        'Datum': 'Date',
        'Beschrijving': 'Description',
        'Titel': 'Title',
        'Prijs': 'Price',
        'Budget': 'Budget',
        'Disciplines': 'Disciplines',
        'Specialisaties': 'Specialisations',
        'Beschikbaarheid': 'Availability',
        'Ervaring': 'Experience',
        'Website': 'Website',
        'Telefoonnummer': 'Phone number',

        // ===== INDEX PAGINA =====
        'Vind jouw perfecte kart coach': 'Find your perfect kart coach',
        'Of plaats een aanvraag': 'Or post a request',
        'Zoek een coach': 'Find a coach',
        'Hoe werkt het?': 'How does it work?',
        'Registreer je': 'Register',
        'Maak een account aan': 'Create an account',
        'Maak een gratis account aan': 'Create a free account',
        'Vind een coach of rijder': 'Find a coach or rider',
        'Plan je sessie': 'Plan your session',
        'Stap 1': 'Step 1',
        'Stap 2': 'Step 2',
        'Stap 3': 'Step 3',
        'Rijder': 'Rider',
        'Coach': 'Coach',
        'Rijders': 'Riders',
        'Voor rijders': 'For riders',
        'Voor coaches': 'For coaches',
        'Meer lezen': 'Read more',
        'Bekijk alle coaches': 'View all coaches',
        // Homepage (nieuw)
        'Meldingen op je telefoon': 'Notifications on your phone',
        '📱 Pushmeldingen': '📱 Push notifications',
        'Krijg een melding van RaceCoachFinder op je telefoon of computer bij een nieuw bericht, groepsbericht of uitnodiging. Je hebt geen app of telefoonnummer nodig.': 'Get a notification from RaceCoachFinder on your phone or computer for a new message, group message or invitation. No app or phone number needed.',
        '✓ Meldingen staan aan op dit apparaat': '✓ Notifications are on for this device',
        'Meldingen staan uit op dit apparaat': 'Notifications are off for this device',
        'Testmelding sturen': 'Send test notification',
        'Uitzetten': 'Turn off',
        '🔔 Meldingen aanzetten': '🔔 Turn on notifications',
        'Eerst op je beginscherm zetten': 'Add to your home screen first',
        'Meldingen zijn geblokkeerd': 'Notifications are blocked',
        'Niet beschikbaar': 'Not available',
        '🔔 Zet meldingen aan op je telefoon': '🔔 Turn on notifications on your phone',
        'Vind jouw': 'Find your',
        'racecoach': 'race coach',
        'Eén platform voor rijders en coaches: vind een coach, regel persoonlijke begeleiding of doe mee aan groepscoaching. Alles via chat.': 'One platform for riders and coaches: find a coach, arrange personal coaching or join group coaching. All through chat.',
        'Word coach': 'Become a coach',
        'Groepscoaching': 'Group Coaching',
        'Open aanvragen': 'Open requests',
        'Welkom terug,': 'Welcome back,',
        'Snel naar:': 'Quick links:',
        'Wat wil je doen?': 'What would you like to do?',
        'Coach zoeken': 'Find a coach',
        'Bekijk profielen, ervaring, prijzen en reviews van racecoaches en neem direct contact op.': 'View profiles, experience, prices and reviews of race coaches and get in touch directly.',
        'Naar coaches': 'Go to coaches',
        'Rijders plaatsen een aanvraag voor één-op-één begeleiding. Coaches bieden aan wanneer en waar ze beschikbaar zijn.': 'Riders post a request for one-to-one coaching. Coaches offer when and where they are available.',
        'Naar persoonlijk coachen': 'Go to personal coaching',
        'Doe mee met een training, wedstrijddag of evenement met meerdere rijders, soms met meerdere coaches.': 'Join a training, race day or event with several riders, sometimes with several coaches.',
        'Naar groepscoaching': 'Go to group coaching',
        'Chat één-op-één of in een groep, ontvang uitnodigingen en betaal facturen veilig online.': 'Chat one-to-one or in a group, receive invitations and pay invoices securely online.',
        'Naar berichten': 'Go to messages',
        'coach beschikbaar': 'coach available',
        'coaches beschikbaar': 'coaches available',
        'komende training': 'upcoming training',
        'komende trainingen': 'upcoming trainings',
        'rijder zoekt een coach': 'rider is looking for a coach',
        'rijders zoeken een coach': 'riders are looking for a coach',
        'ongelezen bericht': 'unread message',
        'ongelezen berichten': 'unread messages',
        'Geen ongelezen berichten': 'No unread messages',
        'Log in om je berichten te zien': 'Log in to see your messages',
        'Zoek en vergelijk coaches op locatie, voertuig en prijs': 'Search and compare coaches by location, vehicle and price',
        'Voertuig': 'Vehicle',
        'Voertuig *': 'Vehicle *',
        'Kies een voertuig': 'Choose a vehicle',
        'Plaats een aanvraag als je een coach zoekt voor een training of wedstrijd': 'Post a request when you need a coach for a training or race',
        'Sluit je aan bij groepscoaching': 'Join group coaching',
        'Chat met coaches en betaal facturen veilig online': 'Chat with coaches and pay invoices securely online',
        'Laat na je coaching een review achter': 'Leave a review after your coaching',
        'Gratis voor rijders': 'Free for riders',
        'Gratis account maken': 'Create a free account',
        'Maak een profiel met je ervaring, specialisaties en prijzen': 'Create a profile with your experience, specialisations and prices',
        'Reageer op rijders die een coach zoeken': 'Respond to riders looking for a coach',
        'Organiseer groepscoaching, ook samen met andere coaches': 'Organise group coaching, also together with other coaches',
        'Stuur betaalverzoeken en facturen vanuit de chat': 'Send payment requests and invoices from the chat',
        'Houd je afspraken bij in je eigen agenda': 'Keep track of your appointments in your own calendar',
        '€10 per maand · maandelijks opzegbaar': '€10 per month · cancel monthly',
        'Eerste maand €5, daarna €10 per maand · maandelijks opzegbaar': 'First month €5, then €10 per month · cancel monthly',
        'Komende groepscoaching': 'Upcoming group coaching',
        'Bekijk alle groepscoaching': 'View all group coaching',
        'Aanbevolen voor jou': 'Recommended for you',
        'Coaches die passen bij jouw specialisaties en niveau': 'Coaches that match your specialisations and level',
        'Alle coaches bekijken': 'View all coaches',
        'Recente coaches': 'Recent coaches',
        'Coaches laden...': 'Loading coaches…',
        'Zo werkt het': 'How it works',
        'Maak een account': 'Create an account',
        'Kies of je rijder of coach bent en vul je profiel in.': 'Choose whether you are a rider or coach and fill in your profile.',
        'Vind elkaar': 'Find each other',
        'Zoek een coach, plaats een aanvraag of meld je aan voor groepscoaching.': 'Find a coach, post a request or sign up for group coaching.',
        'Regel alles via chat': 'Arrange everything through chat',
        'Spreek af, ontvang de factuur en betaal veilig online.': 'Make arrangements, receive the invoice and pay securely online.',
        'Meer uitleg over hoe het werkt': 'More about how it works',
        'Naar mijn dashboard': 'Go to my dashboard',
        '💬 Berichten': '💬 Messages',
        '👥 Training aanmaken': '👥 Create training',
        '🎯 Rijders zoeken een coach': '🎯 Riders looking for a coach',
        'Rijders zoeken een coach': 'Riders looking for a coach',
        '🎯 Aanvraag plaatsen': '🎯 Post a request',
        '👥 Groepscoaching': '👥 Group coaching',
        'Coaches konden niet geladen worden. Probeer het later opnieuw.': 'Coaches could not be loaded. Please try again later.',
        'Nog geen coaches beschikbaar.': 'No coaches available yet.',
        'Word de eerste coach': 'Become the first coach',

        // ===== COACHES PAGINA =====
        'Alle coaches': 'All coaches',
        'Geen coaches gevonden': 'No coaches found',
        'Coach bekijken': 'View coach',
        'Contact opnemen': 'Get in touch',
        'Profiel bekijken': 'View profile',
        'Bekijk profiel': 'View profile',
        'Over mij': 'About me',
        'Tarieven': 'Rates',
        'Reviews': 'Reviews',
        'Stuur bericht': 'Send message',
        'Profiel bewerken': 'Edit profile',
        'Locatie(s)': 'Location(s)',
        'Per uur': 'Per hour',
        'Per dag': 'Per day',

        // ===== PERSOONLIJK COACHEN =====
        'Rijders en coaches vinden elkaar voor één-op-één begeleiding op training of wedstrijd.': 'Riders and coaches connect for one-on-one coaching at training sessions or races.',
        '🙋 Rijder zoekt coach': '🙋 Rider seeks coach',
        '🏁 Coach biedt aan': '🏁 Coach available',
        'Rijder zoekt coach': 'Rider seeks coach',
        'Coach biedt aan': 'Coach available',
        'Aanvraag plaatsen': 'Post request',
        'Aanbieding plaatsen': 'Post offer',
        '+ Aanvraag plaatsen': '+ Post request',
        '+ Aanbieding toevoegen': '+ Add offer',
        '✕ Sluiten': '✕ Close',
        'Soort opdracht': 'Type',
        'Soort aanbieding': 'Type of offer',
        'Soort wedstrijd': 'Race type',
        'Soort': 'Type',
        'Training': 'Training',
        'Wedstrijd': 'Race',
        '🎯 Training': '🎯 Training',
        '🏁 Wedstrijd': '🏁 Race',
        'Organisatie / competitie': 'Organisation / competition',
        'Wedstrijd / circuit': 'Race / circuit',
        'Locatie / kartbaan': 'Location / kart track',
        'Locatie / baan': 'Location / track',
        'Locatie / baan *': 'Location / track *',
        'Datum *': 'Date *',
        'Maximaal budget (€)': 'Maximum budget (€)',
        'Wat wil je aanpakken?': 'What do you want to work on?',
        'Categorie kart': 'Kart category',
        'Reageren': 'Respond',
        'Jouw verzoek': 'Your request',
        'Groep zoekt coach voor een training': 'Group wants a coach for a training',
        'Groep zoekt coach voor een wedstrijd': 'Group wants a coach for a race',
        'Meer info': 'More info',
        'Jouw aanvraag': 'Your request',
        'Coach gezocht voor een wedstrijd': 'Coach wanted for a race',
        'Coach gezocht voor een training': 'Coach wanted for a training',
        'flexibel': 'flexible',
        'Coach uitnodigen': 'Invite coach',
        'Prijs (€)': 'Price (€)',
        'Mijn aanvragen': 'My requests',
        'Mijn aanbiedingen': 'My offers',
        'Rijders die een coach zoeken': 'Riders looking for a coach',
        'Coaches die beschikbaar zijn': 'Available coaches',
        'Aanvraag geplaatst!': 'Request posted!',
        'Aanbieding geplaatst!': 'Offer posted!',
        'Nog geen aanvragen geplaatst.': 'No requests posted yet.',
        'Nog geen persoonlijke coachingaanbiedingen.': 'No personal coaching offers yet.',
        'Geen aanvragen gevonden.': 'No requests found.',
        'Geen aanvragen': 'No requests',
        'Geen resultaten': 'No results',
        'Geen aanbiedingen': 'No offers',
        'Kon aanvragen niet laden.': 'Could not load requests.',
        'Kon aanbiedingen niet laden.': 'Could not load offers.',
        'Aanvraag verwijderen': 'Delete request',
        'Heb je een coach gevonden via RaceCoachFinder?': 'Did you find a coach through RaceCoachFinder?',
        '✅ Ja, ik heb een coach gevonden!': '✅ Yes, I found a coach!',
        'Nee, verwijder gewoon': 'No, just delete it',
        '✓ Coach gevonden': '✓ Coach found',
        'Je hebt nog geen persoonlijke aanvragen geplaatst.': 'You have not posted any personal requests yet.',
        'Aanvragen kunnen niet worden geladen.': 'Requests could not be loaded.',
        'Nog geen persoonlijke aanbiedingen geplaatst.': 'No personal offers posted yet.',
        'Aanbiedingen kunnen niet worden geladen.': 'Offers could not be loaded.',
        '(optioneel)': '(optional)',
        '(optioneel – zoek een specifieke coach)': '(optional – search for a specific coach)',

        // ===== CHIP / BADGE LABELS =====
        '🏁 Wedstrijd': '🏁 Race',
        '🎯 Training': '🎯 Training',
        '👥 Groep': '👥 Group',
        'Sprint': 'Sprint',
        'Endurance': 'Endurance',
        'Regenrijden': 'Wet driving',
        'Snelheid': 'Speed',
        'Basis': 'Basics',
        'Inhalen': 'Overtaking',
        'Verdedigen': 'Defending',
        'Kwalificatie': 'Qualifying',
        'Race brain': 'Race brain',
        'Rijstijl': 'Driving style',
        'Rempunten': 'Braking points',
        'Algemeen': 'General',
        'Huurkart': 'Rental kart',
        'Rotax Max': 'Rotax Max',
        'OK / Junior': 'OK / Junior',
        'KZ / Shifter': 'KZ / Shifter',
        'Superkart': 'Superkart',
        'Simracing': 'Simracing',
        '🏎️ Sprint': '🏎️ Sprint',
        '⏱️ Endurance': '⏱️ Endurance',

        // ===== GROEPSTRAINING =====
        '👥 Groepscoaching': '👥 Group Coaching',
        'Coaches die groepssessies aanbieden en rijders die een groep zoeken.': 'Coaches offering group sessions and riders looking for a group.',
        '🏁 Coach biedt groep aan': '🏁 Coach offers group',
        '🙋 Rijder zoekt coach': '🙋 Rider seeks coach',
        'Coach biedt groep aan': 'Coach offers group',
        'Competitie': 'Competition',
        'Niveau rijders': 'Rider level',
        '🗺️ Kaart openen': '🗺️ Open map',
        'Maakt niet uit': 'Any level',
        'Alle niveaus': 'All levels',
        'Eerste keer': 'First time',
        'Gevorderd': 'Intermediate',
        'Ervaren': 'Experienced',
        'Zeer ervaren': 'Very experienced',
        'Professioneel': 'Professional',
        'Groepsgrootte': 'Group size',
        'deelnemers': 'participants',
        'Max. deelnemers': 'Max. participants',
        'Deelnemers': 'Participants',
        '+ Groepssessie toevoegen': '+ Add group session',
        '+ Verzoek plaatsen': '+ Post request',
        'Verzoek geplaatst!': 'Request posted!',
        'Coaches die groepssessies aanbieden': 'Coaches offering group sessions',
        'Rijders die een groep zoeken': 'Riders looking for a group',
        'Nog geen groepsaanbiedingen.': 'No group offers yet.',
        'Nog geen groepsverzoeken geplaatst.': 'No group requests posted yet.',
        'Nog geen groepssessies beschikbaar.': 'No group sessions available yet.',
        'Geen verzoeken gevonden.': 'No requests found.',
        'Kon groepssessies niet laden.': 'Could not load group sessions.',
        'Kon groepsverzoeken niet laden.': 'Could not load group requests.',
        'Je hebt nog geen groepsverzoeken geplaatst.': 'You have not posted any group requests yet.',
        'Groepsverzoek aanmaken': 'Create group request',
        'Locatie *': 'Location *',
        'Wat voor soort sessie zoek je?': 'What type of session are you looking for?',
        'Max. budget (€)': 'Max. budget (€)',

        // ===== DASHBOARD RIJDER =====
        'Welkom terug': 'Welcome back',
        'Mijn dashboard': 'My dashboard',
        'Mijn profiel': 'My profile',
        'Aanvragen': 'Requests',
        'Aanbiedingen': 'Offers',
        'Persoonlijk Coachen': 'Personal Coaching',
        'Zoek een persoonlijke coach': 'Find a personal coach',
        'Groep zoeken': 'Find a group',
        'Zoek groepscoaching': 'Find group coaching',
        'Bekijk mijn aanvragen': 'View my requests',
        'Nieuwe aanvraag': 'New request',

        // ===== DASHBOARD COACH =====
        'Mijn aanbiedingen beheren': 'Manage my offers',
        'Nieuwe aanbieding': 'New offer',
        'Uitnodigingen': 'Invitations',
        'Deelnemers beheren': 'Manage participants',
        'Uitnodiging accepteren': 'Accept invitation',
        'Uitnodiging weigeren': 'Decline invitation',
        'Geaccepteerd': 'Accepted',
        'Geweigerd': 'Declined',
        'In behandeling': 'Pending',

        // ===== BERICHTEN =====
        'Geen berichten': 'No messages',
        'Geen gesprekken': 'No conversations',
        'Bericht sturen': 'Send message',
        'Berichtje sturen naar': 'Send a message to',
        'Ongelezen': 'Unread',
        'Bericht verzonden': 'Message sent',

        // ===== INLOGGEN / REGISTREREN =====
        'Inloggen bij RaceCoachFinder': 'Log in to RaceCoachFinder',
        'Nog geen account?': 'No account yet?',
        'Al een account?': 'Already have an account?',
        'Wachtwoord vergeten?': 'Forgot your password?',
        'Ik ben een:': 'I am a:',
        'Rijder (ik wil een coach vinden)': 'Rider (I want to find a coach)',
        'Coach (ik wil rijders begeleiden)': 'Coach (I want to guide riders)',
        'Beide (rijder én coach)': 'Both (rider and coach)',

        // ===== INSTELLINGEN =====
        'Account instellingen': 'Account settings',
        'Wachtwoord wijzigen': 'Change password',
        'Account verwijderen': 'Delete account',
        'Wijzigingen opslaan': 'Save changes',
        'Profiel opgeslagen': 'Profile saved',
        'Fout bij opslaan': 'Error saving',

        // ===== ACCOUNT INRICHTEN =====
        'Account instellen': 'Set up account',
        'Jouw profiel': 'Your profile',
        'Voeg een foto toe': 'Add a photo',
        'Profielfoto': 'Profile photo',
        'Kies een foto': 'Choose a photo',
        'Foto uploaden': 'Upload photo',
        'Sla op en ga verder': 'Save and continue',

        // ===== COACH BEWERKEN =====
        'Profiel aanpassen': 'Edit profile',
        'Sla profiel op': 'Save profile',
        'Specialisaties toevoegen': 'Add specialisations',
        'Mijn locaties': 'My locations',
        'Locatie toevoegen': 'Add location',
        'Tarief instellen': 'Set rate',

        // ===== RAPPORTEER / REPORT =====
        'Rapporteren': 'Report',
        'Coach rapporteren': 'Report coach',
        'Reden voor rapportage': 'Reason for report',
        'Verstuur rapport': 'Submit report',

        // ===== FOUTMELDINGEN / STATUSBERICHTEN =====
        'Er is iets misgegaan.': 'Something went wrong.',
        'Probeer het opnieuw.': 'Please try again.',
        'Kon gegevens niet laden.': 'Could not load data.',
        'Sessie verlopen. Log opnieuw in.': 'Session expired. Please log in again.',
        'Geen internetverbinding.': 'No internet connection.',
        'open aanvraag': 'open request',
        'open aanvragen': 'open requests',
        'beschikbaar': 'available',

        // ===== HOE WERKT HET =====
        'Hoe werkt RaceCoachFinder?': 'How does RaceCoachFinder work?',
        'Voor rijders': 'For riders',
        'Voor coaches': 'For coaches',

        // ===== ADMIN =====
        'Gebruikers': 'Users',
        'Rapportages': 'Reports',
        'Beheer': 'Admin',
        'Alle gebruikers': 'All users',
        'Geblokkeerd': 'Blocked',
        'Deactiveren': 'Deactivate',

        // ===== BETALINGEN =====
        'Betalen': 'Pay',
        'Betaling geslaagd': 'Payment successful',
        'Betaling mislukt': 'Payment failed',
        'Ga naar betalen': 'Proceed to payment',
        'Terug naar dashboard': 'Back to dashboard',

        // ===== FILTER LABELS (persoonlijk coachen) =====
        'Filteren op locatie...': 'Filter by location…',
        '(optioneel – zoek een specifieke coach)': '(optional – search for a specific coach)',

        // ===== PLACEHOLDER TEKSTEN =====
        'bijv. Rotax Max Euro Challenge': 'e.g. Rotax Max Euro Challenge',
        'bijv. Ronde 3 – Genk': 'e.g. Round 3 – Genk',
        'bijv. Karting Genk, Mariembourg...': 'e.g. Karting Genk, Mariembourg…',
        'bijv. Karting Genk': 'e.g. Karting Genk',
        'bijv. 200': 'e.g. 200',
        'bijv. 150': 'e.g. 150',
        'excl. reiskosten': 'excl. travel costs',
        'Wat verwacht je van de coach? Wat is je niveau?...': 'What do you expect from the coach? What is your level?…',
        'bijv. Persoonlijke coaching Rotax Max – trainingssessie': 'e.g. Personal Rotax Max coaching – training session',
        'bijv. Coach beschikbaar bij NK Karting Genk': 'e.g. Coach available at NK Karting Genk',
        'Beschrijf wat je biedt, je aanpak, wat inbegrepen is...': 'Describe what you offer, your approach, what is included…',
        'Zoek op naam...': 'Search by name…',
        'Zoek een coach...': 'Find a coach…',
        'Zoek op naam, locatie of specialisatie...': 'Search by name, location or specialisation…',
        'Schrijf iets over jezelf...': 'Write something about yourself…',
        'Jouw bericht...': 'Your message…',
        'Beschrijf jezelf, je ervaring en aanpak...': 'Describe yourself, your experience and approach…',
        'bijv. Genk, Mariembourg...': 'e.g. Genk, Mariembourg…',
        'bijv. 3': 'e.g. 3',
        'bijv. 8': 'e.g. 8',
        'bijv. jouw.naam@email.com': 'e.g. your.name@email.com',

        // ===== Wachtlijst, account, e-mailtaal (november 2026) =====
        "Hiermee verwijder je je account en alles wat erbij hoort: je profiel, berichten, groepsberichten, boekingen, reviews, aanvragen en aanbiedingen.\n                Een lopend abonnement wordt opgezegd. Dit kan niet ongedaan worden gemaakt.": "This deletes your account and everything that belongs to it: your profile, messages, group messages, bookings, reviews, requests and offers. An active subscription will be cancelled. This cannot be undone.",
        "Al je gegevens worden direct verwijderd, ook je berichten en boekingen. Dit kan niet ongedaan worden gemaakt.\n            Vul ter bevestiging je wachtwoord in.": "All your data will be deleted immediately, including your messages and bookings. This cannot be undone. Enter your password to confirm.",
        "👥 Nieuwe groep": "👥 New group",
        "Groep maken": "Create group",
        '🎉 Alle coaches hebben geaccepteerd. De training staat nu online!': '🎉 All coaches have accepted. The training is now online!',
        'Vol — max. aantal rijders bereikt': 'Full — maximum number of riders reached',
        '🔔 Meld me als er een plek vrijkomt': '🔔 Notify me when a spot opens up',
        '✓ Je krijgt een melding': '✓ You will be notified',
        '💬 Naar de groepschat': '💬 Go to the group chat',
        'Je krijgt een melding zodra er een plek vrijkomt.': 'You will be notified as soon as a spot opens up.',
        'Taal van e-mails': 'Email language',
        'In welke taal wil je de mails van RaceCoachFinder ontvangen?': 'In which language do you want to receive emails from RaceCoachFinder?',
        'Wachtwoord wijzigen': 'Change password',
        'Huidig wachtwoord': 'Current password',
        'Nieuw wachtwoord': 'New password',
        'Herhaal nieuw wachtwoord': 'Repeat new password',
        'Huidig wachtwoord vergeten?': 'Forgot your current password?',
        'Account verwijderen': 'Delete account',
        'Account verwijderen…': 'Delete account…',
        'Account definitief verwijderen?': 'Permanently delete account?',
        'Definitief verwijderen': 'Delete permanently',
        'Je wachtwoord': 'Your password',
        '✓ Je wachtwoord is gewijzigd.': '✓ Your password has been changed.',
        'De nieuwe wachtwoorden zijn niet hetzelfde.': 'The new passwords do not match.',
        'Je account en al je gegevens zijn verwijderd.': 'Your account and all your data have been deleted.',
        'Je kunt een review schrijven nadat je een coaching met deze coach hebt betaald en als gedaan hebt bevestigd.': 'You can write a review after you have paid for a coaching session with this coach and confirmed it took place.',
        'Automatisch, per rijder': 'Automatic, per rider',
        'Wordt opgehaald…': 'Loading…',
        'Automatisch': 'Automatic',

        // ===== Aanvullingen (controle november 2026) =====
        "Coaches zoeken": "Search coaches",
        "Max. prijs (goedkoopste pakket)": "Max. price (cheapest package)",
        "Alle prijzen": "All prices",
        "Tot €50": "Up to €50",
        "Tot €100": "Up to €100",
        "Tot €150": "Up to €150",
        "Tot €200": "Up to €200",
        "Alleen online coaching": "Online coaching only",
        "★ Alleen favorieten": "★ Favourites only",
        "Prijs laag → hoog": "Price low → high",
        "Prijs hoog → laag": "Price high → low",
        "bijv. Jan de Vries": "e.g. John Smith",
        "Coaches die beschikbaar zijn voor een groepssessie. Als rijder sluit je je aan bij een training, wedstrijddag of evenement.": "Coaches available for a group session. As a rider you join a training, race day or event.",
        "Hoe lang duurt de race?": "How long is the race?",
        "Kies de duur…": "Choose the duration…",
        "Max. rijders": "Max. riders",
        "Prijs per rijder (€)": "Price per rider (€)",
        "Gratis voor rijder": "Free for rider",
        "– zichtbaar op de kaart (max 200 tekens)": "– visible on the card (max 200 characters)",
        "(optioneel) – zichtbaar via popup": "(optional) – visible in the popup",
        "Datum vanaf": "Date from",
        "Betaald": "Paid",
        "Datum (vroegst eerst)": "Date (earliest first)",
        "👥 Reageren op activiteit": "👥 Respond to activity",
        "Met hoeveel rijders ben je al?": "How many riders are you already?",
        "De coach kan later kiezen of er nog meer rijders bij mogen.": "The coach can decide later whether more riders can join.",
        "Max. budget per rijder (€)": "Max. budget per rider (€)",
        "Wat zoeken jullie? *": "What are you looking for? *",
        "Rijders uitnodigen": "Invite riders",
        "Mijn groepsverzoeken": "My group requests",
        "Wil werken aan": "Wants to work on",
        "Groepen die een coach zoeken": "Groups looking for a coach",
        "geen maximum": "no maximum",
        "bijv. Coach beschikbaar voor begeleiding op wedstrijddag in Genk...": "e.g. Coach available for race day support in Genk...",
        "Beschrijf uitgebreid wat je biedt, wat je verwacht van de rijder, programma van de dag, wat inbegrepen is, etc.": "Describe in detail what you offer, what you expect from the rider, the programme of the day, what is included, etc.",
        "Typ naam om een coach te zoeken...": "Type a name to search for a coach...",
        "bijv. Met 4 vrienden regenrijden leren": "e.g. Learning to drive in the rain with 4 friends",
        "Beschrijf de groep: niveau, wat jullie willen leren of bereiken...": "Describe the group: level, what you want to learn or achieve...",
        "Naam rijder zoeken...": "Search rider name...",
        "Coach zoeken...": "Search coach...",
        "bijv. Hulp bij inhalen op Karting Genk": "e.g. Help with overtaking at Karting Genk",
        "In drie stappen verbind je met de beste racecoach voor jouw situatie.": "Connect with the best race coach for your situation in three steps.",
        "Maak een profiel aan": "Create a profile",
        "Registreer gratis en vul je rijdersprofiel in: niveau, specialisaties, doelen en beschikbare banen. Coaches kunnen dan zien wie je bent.": "Register for free and fill in your rider profile: level, specialisations, goals and tracks. Coaches can then see who you are.",
        "Blader door coaches en filter op locatie, voertuig of online coaching. Bekijk hun ervaring, resultaten en tarieven.": "Browse coaches and filter by location, vehicle or online coaching. View their experience, results and rates.",
        "Stuur een bericht": "Send a message",
        "Neem direct contact op via de berichtenservice. Bespreek je wensen, plan een sessie in en ga aan de slag.": "Get in touch directly through the messaging service. Discuss your wishes, plan a session and get started.",
        "Alles wat je wil weten over RaceCoachFinder": "Everything you want to know about RaceCoachFinder",
        "Is RaceCoachFinder gratis te gebruiken?": "Is RaceCoachFinder free to use?",
        "Voor een rijder is het gratis te gebruiken. Je betaalt alleen voor de coaching die je afneemt — dat regel je direct met de coach.": "It is free for riders. You only pay for the coaching you book — you arrange that directly with the coach.",
        "Hoe weet ik of een coach betrouwbaar is?": "How do I know if a coach is reliable?",
        "Elke coach vult zijn eigen profiel in met ervaring, resultaten en specialisaties. Na een sessie kunnen rijders een review achterlaten. Lees die reviews om een goed beeld te krijgen.": "Every coach fills in their own profile with experience, results and specialisations. After a session riders can leave a review. Read those reviews to get a good picture.",
        "Kan ik ook een coach zoeken als ik net begin met karten?": "Can I look for a coach if I am just starting with karting?",
        "Wat is online coaching?": "What is online coaching?",
        "Sommige coaches bieden begeleiding op afstand aan — denk aan data-analyse, video-review, setup advies of technische coaching via video call. Filter op \"Online coaching\" om die coaches te vinden.": "Some coaches offer remote coaching — think of data analysis, video review, setup advice or technical coaching via video call. Filter on \"Online coaching\" to find those coaches.",
        "Hoe werkt het betalen?": "How does payment work?",
        "Ik ben coach. Hoe meld ik me aan?": "I am a coach. How do I sign up?",
        "Registreer als \"Coach\", vul je profiel in met je ervaring, beschikbaarheid en tarieven, en publiceer je profiel. Rijders kunnen je dan vinden en contact opnemen.": "Register as \"Coach\", fill in your profile with your experience, availability and rates, and publish your profile. Riders can then find you and get in touch.",
        "Klaar om te beginnen?": "Ready to get started?",
        "Maak gratis een account aan en vind jouw coach.": "Create a free account and find your coach.",
        "Account aanmaken": "Create account",
        "Ik wil mij aanmelden als:": "I want to sign up as:",
        "Ik zoek een coach": "I am looking for a coach",
        "Ik bied coaching aan": "I offer coaching",
        "Ik ben 18 jaar of ouder. Ik ga akkoord met de": "I am 18 or older. I agree to the",
        "Zichtbaar voor alle rijders op het platform": "Visible to all riders on the platform",
        "Onbeperkt berichten en boekingen ontvangen": "Receive unlimited messages and bookings",
        "Eigen coachprofiel met foto en statistieken": "Your own coach profile with photo and statistics",
        "Beoordelingen en reviews van rijders": "Ratings and reviews from riders",
        "Na het aanmaken van je account bevestig je eerst je e-mailadres. Daarna word je automatisch doorgestuurd naar Mollie voor veilige betaling via": "After creating your account you first confirm your email address. You will then be redirected to Mollie for secure payment via",
        ", creditcard, Bancontact en meer.": ", credit card, Bancontact and more.",
        "Ik geef RaceCoachFinder toestemming mijn naam en openbaar beschikbare foto's te gebruiken voor promotie op social media.": "I give RaceCoachFinder permission to use my name and publicly available photos for promotion on social media.",
        "Dit doen we ook om jou te helpen, zodat er meer rijders op het platform voor jou komen.": "We also do this to help you, so that more riders come to the platform for you.",
        "Wij mogen je bijvoorbeeld verwelkomen op ons Instagram-account als nieuwe coach — denk aan een bericht als \"We hebben een nieuwe coach erbij!\" met je naam en een openbare foto. Je kunt dit later altijd intrekken via je accountinstellingen.": "For example, we may welcome you on our Instagram account as a new coach — think of a post like \"We have a new coach!\" with your name and a public photo. You can withdraw this at any time in your account settings.",
        "Volgende →": "Next →",
        "jouw@email.com": "your@email.com",
        "Inloggen met uitnodigingscode": "Log in with invitation code",
        "Heb je een vraag, opmerking of wil je samenwerken? Stuur ons een bericht en we reageren zo snel mogelijk.": "Do you have a question, comment or want to work together? Send us a message and we will respond as soon as possible.",
        "Jouw naam": "Your name",
        "Waar gaat je bericht over?": "What is your message about?",
        "Schrijf hier je bericht...": "Write your message here...",
        "Haal het meeste uit RaceCoachFinder — of je nu rijder of coach bent.": "Get the most out of RaceCoachFinder — whether you are a rider or a coach.",
        "Welkom bij RaceCoachFinder!": "Welcome to RaceCoachFinder!",
        "Je account is aangemaakt. Lees de tips hieronder om het beste uit het platform te halen.": "Your account has been created. Read the tips below to get the most out of the platform.",
        "Ga naar mijn dashboard": "Go to my dashboard",
        "Tips voor coaches": "Tips for coaches",
        "Zo word je beter gevonden en haal je meer uit het platform.": "How to be found more easily and get more out of the platform.",
        "Geef je trainingen via ons platform? Tag ons op Instagram. Zo krijg jij extra zichtbaarheid via onze kanalen, en helpt het mee om meer rijders naar het platform te trekken — meer rijders betekent meer potentiële klanten voor jou.": "Do you give trainings through our platform? Tag us on Instagram. You get extra visibility through our channels, and it helps bring more riders to the platform — more riders means more potential clients for you.",
        "Wees gedetailleerd op je profiel": "Be detailed on your profile",
        "Beschrijf precies wie je bent: je achtergrond, welke disciplines je coacht, op welke banen je actief bent en wat een rijder van jou kan verwachten. Een uitgebreid profiel wekt meer vertrouwen en trekt de juiste rijders aan.": "Describe exactly who you are: your background, which disciplines you coach, which tracks you are active at and what a rider can expect from you. A detailed profile builds more trust and attracts the right riders.",
        "Vraag rijders om een recensie": "Ask riders for a review",
        "Na elke sessie is dit het moment. Coaches met meer positieve reviews worden beter zichtbaar in de zoekresultaten. Een simpel verzoek aan het einde van een training kan het verschil maken.": "After every session is the moment. Coaches with more positive reviews are more visible in the search results. A simple request at the end of a training can make the difference.",
        "Doe elke coaching via het platform": "Do all coaching through the platform",
        "Hoe meer sessies via RaceCoachFinder lopen, hoe meer reviews je opbouwt. Dat maakt je betrouwbaarder voor nieuwe rijders die je nog niet kennen — vertrouwen bouw je op door zichtbare resultaten.": "The more sessions run through RaceCoachFinder, the more reviews you build up. That makes you more trustworthy for new riders who do not know you yet — trust is built through visible results.",
        "Zet jezelf beschikbaar voor evenementen": "Make yourself available for events",
        "Via de": "On the",
        "kun je aangeven dat je beschikbaar bent op een specifieke datum of bij een bepaald evenement. Ben je sowieso bij een wedstrijd? Dan kun je meerdere rijders tegelijk coachen — ook voor een scherpere prijs.": "you can show that you are available on a specific date or at a certain event. Are you at a race anyway? Then you can coach several riders at once — also for a sharper price.",
        "Deel je profielpagina": "Share your profile page",
        "Kopieer de link naar jouw coachprofiel en deel hem op WhatsApp, Instagram of je eigen website. Rijders die jou al kennen kunnen dan direct boeken — en nieuwe rijders komen zo gemakkelijk bij je terecht.": "Copy the link to your coach profile and share it on WhatsApp, Instagram or your own website. Riders who already know you can book directly — and new riders find you easily.",
        "Tips voor rijders": "Tips for riders",
        "Vind de juiste coach en haal het maximale uit elke sessie.": "Find the right coach and get the most out of every session.",
        "Schrijf een recensie na elke sessie": "Write a review after every session",
        "Laat een eerlijke review achter. Het helpt andere rijders de juiste coach te vinden, en coaches die goed werk leveren verdienen die erkenning. Bovendien maakt het het platform beter voor iedereen.": "Leave an honest review. It helps other riders find the right coach, and coaches who do good work deserve that recognition. It also makes the platform better for everyone.",
        "Vul je profiel volledig in": "Fill in your profile completely",
        "Vermeld je niveau, welke disciplines je rijdt, je doelen en de banen waar je actief bent. Hoe meer een coach van je weet, hoe beter ze zich kunnen voorbereiden op jouw sessie.": "Mention your level, which disciplines you race, your goals and the tracks where you are active. The more a coach knows about you, the better they can prepare for your session.",
        "Lees reviews van andere rijders": "Read reviews from other riders",
        "Reviews van rijders op jouw niveau geven de meeste waarde. Kijk niet alleen naar het gemiddelde, maar lees ook wat er concreet wordt gezegd over de stijl van coachen.": "Reviews from riders at your level are the most valuable. Do not only look at the average, but also read what is actually said about the coaching style.",
        "Wees specifiek over je leerdoelen": "Be specific about your learning goals",
        "\"Ik wil mijn remtechniek in sector 2 verbeteren\" is veel bruikbaarder voor een coach dan \"ik wil sneller worden\". Hoe concreter jij bent, hoe gerichter de sessie wordt.": "\"I want to improve my braking in sector 2\" is much more useful for a coach than \"I want to be faster\". The more concrete you are, the more focused the session becomes.",
        "Boek op tijd bij drukke weekenden": "Book early for busy weekends",
        "Goede coaches zijn snel volgeboekt rondom grote wedstrijden. Plan je sessie ruim van tevoren in, zodat je de coach krijgt die echt bij jou past.": "Good coaches are quickly fully booked around big races. Plan your session well in advance, so you get the coach that really suits you.",
        "Probeer meerdere coaches uit": "Try several coaches",
        "Iedere coach heeft een eigen stijl. Het kan lonen om een keer met iemand anders te rijden — soms geeft een frisse blik precies de doorbraak die je zocht.": "Every coach has their own style. It can pay off to ride with someone else once — sometimes a fresh view gives exactly the breakthrough you were looking for.",
        "Over deze coach": "About this coach",
        "⚡ Reageert binnen het uur": "⚡ Responds within the hour",
        "☆ Opslaan als favoriet": "☆ Save as favourite",
        "Schrijf een beoordeling": "Write a review",
        "Beoordeling versturen": "Submit review",
        "Rapporteer deze coach": "Report this coach",
        "Beschrijf waarom je deze coach wilt melden. RaceCoachFinder beoordeelt elke melding.": "Describe why you want to report this coach. RaceCoachFinder reviews every report.",
        "Coach niet gevonden.": "Coach not found.",
        "Terug naar overzicht": "Back to overview",
        "Optioneel: vertel meer over je ervaring...": "Optional: tell more about your experience...",
        "Beschrijf het probleem...": "Describe the problem...",
        "Vul je e-mailadres in. Als er een account bij bestaat, sturen we een verificatiecode.": "Enter your email address. If an account exists, we will send a verification code.",
        "Terug naar inloggen": "Back to login",
        "Rijder Dashboard": "Rider Dashboard",
        "📨 Aanvragen": "📨 Requests",
        "Zet een aanvraag online — coaches nemen contact op": "Post a request online — coaches will contact you",
        "Doe mee met een training of wedstrijd, samen met andere rijders": "Join a training or race, together with other riders",
        "Bekijk alle beschikbare racecoaches op het platform": "View all available race coaches on the platform",
        "Rijderprofiel aanmaken": "Create rider profile",
        "Maak een profiel aan zodat coaches meer over jou te weten kunnen komen.": "Create a profile so coaches can learn more about you.",
        "Profiel aanmaken": "Create profile",
        "Klik om foto te wijzigen": "Click to change photo",
        "Mijn boekingen": "My bookings",
        "Nog niet betaald": "Not yet paid",
        "Vertel iets over jezelf als rijder": "Tell something about yourself as a rider",
        "bijv. Mijn ronde-tijden verbeteren, racetechniek leren": "e.g. Improve my lap times, learn racing technique",
        "Selecteer een gesprek om te openen": "Select a conversation to open",
        "Prijs excl.": "Price excl.",
        "+ Regel toevoegen": "+ Add line",
        "Voeg toe aan agenda": "Add to calendar",
        "Als reactie op aanvraag": "In response to request",
        "Hoe was de coaching?": "How was the coaching?",
        "Laat een beoordeling achter (optioneel).": "Leave a review (optional).",
        "🎯 Uitnodiging voor activiteit": "🎯 Invitation for activity",
        "👥 Alle uitgenodigde rijders toevoegen": "👥 Add all invited riders",
        "➕ Lid toevoegen": "➕ Add member",
        "🔍 Zoek een gebruiker om te chatten...": "🔍 Search a user to chat with...",
        "Bijv. bedankt voor de samenwerking!": "E.g. thanks for the collaboration!",
        "Typ een bericht...": "Type a message...",
        "🔍 Zoek op naam...": "🔍 Search by name...",
        "🔍 Zoek gebruikers...": "🔍 Search users...",
        "Beheer je notificaties, berichten en accountinformatie.": "Manage your notifications, messages and account information.",
        "Je hebt eerder geen toestemming gegeven. Zet meldingen voor RaceCoachFinder weer aan in de instellingen van je telefoon of browser (bij iPhone: Instellingen → Meldingen → RaceCoachFinder) en kom dan hier terug.": "You previously did not give permission. Turn notifications for RaceCoachFinder back on in the settings of your phone or browser (on iPhone: Settings → Notifications → RaceCoachFinder) and then come back here.",
        "Zet dit uit om helemaal geen mails te ontvangen.": "Turn this off to receive no emails at all.",
        "Alleen van favorieten": "Only from favourites",
        "Ontvang meldingen alleen van rijders of coaches die je als favoriet hebt.": "Only receive notifications from riders or coaches you have marked as favourite.",
        "Ontvang een mail als een rijder een factuur van jou heeft betaald.": "Receive an email when a rider has paid one of your invoices.",
        "Nieuwe passende aanvragen": "New matching requests",
        "Ontvang een mail als er een aanvraag is die bij jouw specialisaties past.": "Receive an email when there is a request that matches your specialisations.",
        "Nieuwe beoordeling": "New review",
        "Ontvang een mail als een rijder een review op je profiel plaatst.": "Receive an email when a rider posts a review on your profile.",
        "Mails na hoeveel gemiste berichten?": "Emails after how many missed messages?",
        "Je ontvangt een mail zodra je dit aantal berichten hebt gemist.": "You receive an email as soon as you have missed this number of messages.",
        "Ontvang een mail met je agenda-afspraken van die dag.": "Receive an email with your calendar appointments of that day.",
        "Hoever van tevoren?": "How far in advance?",
        "De herinnering wordt elke ochtend om ±8 uur verstuurd.": "The reminder is sent every morning at around 8 o'clock.",
        "Berichten & privacy": "Messages & privacy",
        "Berichten van iedereen": "Messages from everyone",
        "Zet dit uit om alleen berichten van favorieten toe te staan.": "Turn this off to only allow messages from favourites.",
        "Als dit uit staat ben je niet te vinden via de zoekpagina.": "When this is off you cannot be found via the search page.",
        "RaceCoachFinder mag je naam en openbare foto gebruiken om je als coach te promoten (bijv. op Instagram).": "RaceCoachFinder may use your name and public photo to promote you as a coach (e.g. on Instagram).",
        "Wil je je wachtwoord wijzigen?": "Want to change your password?",
        "Je hebt nog geen coachprofiel. Vul hieronder je gegevens in om zichtbaar te worden op het platform.": "You do not have a coach profile yet. Fill in your details below to become visible on the platform.",
        "Favoriete rijders": "Favourite riders",
        "Passende aanvragen": "Matching requests",
        "✅ Je profiel is zichtbaar voor rijders.": "✅ Your profile is visible to riders.",
        "Afspraken deze maand": "Appointments this month",
        "Geen afspraken deze maand.": "No appointments this month.",
        "Afspraak toevoegen": "Add appointment",
        "DATUM & TIJD": "DATE & TIME",
        "Recente aanvragen": "Recent requests",
        "Alle aanvragen →": "All requests →",
        "(optioneel — voor PDF-facturen)": "(optional — for PDF invoices)",
        "Deze gegevens verschijnen automatisch op de facturen die je verstuurt. Vul ze in voor een professioneel resultaat.": "These details appear automatically on the invoices you send. Fill them in for a professional result.",
        "Online coaching beschikbaar": "Online coaching available",
        "Voeg services met prijzen toe, bijv.": "Add services with prices, e.g.",
        "Bijv. Training met rijder...": "E.g. Training with rider...",
        "Sell yourself!! Vertel kort wat over jezelf": "Sell yourself!! Tell us briefly about yourself",
        "Dit kan op rijders en coach vlak zijn bijv. Nederlands kampioen 2018, of welke rijders je tot bepaalde resultaten hebt geholpen.": "This can be about riders and coaching, e.g. Dutch champion 2018, or which riders you helped achieve certain results.",
        "bijv. Oldenzaal, wanneer je geen thuis baan hebt kan je dit ook leeg laten.": "e.g. Oldenzaal; if you do not have a home track you can leave this empty.",
        "Aanvragen passend bij jouw profiel zijn gemarkeerd. Klik op \"Stuur bericht\" om contact op te nemen.": "Requests matching your profile are highlighted. Click \"Send message\" to get in touch.",
        "Rijder zoeken": "Search rider",
        "Datum tot": "Date to",
        "Alleen passend voor mij": "Only matching for me",
        "★ Favoriete rijders": "★ Favourite riders",
        "★ Past bij jouw profiel": "★ Matches your profile",
        "De coach stuurt je een factuur via de chat. Die betaal je veilig online via Mollie (iDEAL, creditcard en meer), rechtstreeks aan de coach. Bovenop het factuurbedrag betaal je 2% servicekosten aan RaceCoachFinder. Na de sessie bevestigen coach en rijder allebei dat de activiteit gedaan is.": "The coach sends you an invoice via the chat. You pay it securely online via Mollie (iDEAL, credit card and more), directly to the coach. On top of the invoice amount you pay a 2% service fee to RaceCoachFinder. After the session coach and rider both confirm that the activity took place.",
        "Absoluut. Er zijn coaches voor alle niveaus, van absolute beginner tot gevorderd rijder. Geef je niveau aan in je profiel, of kijk bij Groepscoaching naar trainingen voor het niveau \"Eerste keer\" of \"Beginner\".": "Absolutely. There are coaches for all levels, from absolute beginner to advanced rider. Enter your level in your profile, or look at Group Coaching for trainings at the level \"First time\" or \"Beginner\".",
        "\"Groepscoaching\"-pagina": "\"Group Coaching\" page",
        "Reageren bij Groepscoaching": "Respond at Group Coaching",
        "Betaalde facturen": "Paid invoices",
        "Promotie op social media": "Promotion on social media",
        "Je gesprekken konden niet worden geladen.": "Your conversations could not be loaded.",
        "Controleer je internetverbinding en probeer het opnieuw.": "Check your internet connection and try again.",
        "De coaches konden niet worden geladen. Controleer je internetverbinding en probeer het opnieuw.": "The coaches could not be loaded. Check your internet connection and try again.",
        "De aanvragen konden niet worden geladen. Probeer het later opnieuw.": "The requests could not be loaded. Please try again later.",
        "Kan geen verbinding maken met de server. Probeer het zo opnieuw.": "Cannot connect to the server. Please try again shortly.",
        "Onjuist e-mailadres of wachtwoord.": "Incorrect email address or password.",
        "Te veel mislukte pogingen. Probeer het over 15 minuten opnieuw of stel een nieuw wachtwoord in.": "Too many failed attempts. Try again in 15 minutes or set a new password.",
        "Wachtwoord moet minimaal 8 tekens zijn.": "Password must be at least 8 characters.",
        "Minimaal 8 tekens": "At least 8 characters",
        "Uitnodiging verstuurd": "Invitation sent",
        "Je hebt al een review voor deze coach geschreven.": "You have already reviewed this coach.",
        "Nieuwe aanbieding plaatsen": "Post a new offer",
        "Beschikbare coaches": "Available coaches",
        // Mollie Connect (betalingen rechtstreeks aan de coach)
        "Rijders betalen je facturen rechtstreeks via Mollie. Koppel je Mollie-account om facturen te kunnen sturen.": "Riders pay your invoices directly via Mollie. Connect your Mollie account to be able to send invoices.",
        "Koppelen": "Connect",
        "Je Mollie-account is nog niet klaar": "Your Mollie account is not ready yet",
        "Bekijken": "View",
        "Betalingen ontvangen": "Receiving payments",
        "Rijders betalen jouw facturen online via Mollie. Het geld komt rechtstreeks op je eigen Mollie-account, en Mollie maakt het over naar je bankrekening.": "Riders pay your invoices online via Mollie. The money goes straight to your own Mollie account, and Mollie transfers it to your bank account.",
        "RaceCoachFinder rekent 2% servicekosten. De rijder betaalt die bovenop je factuurbedrag, en ze gaan automatisch naar RaceCoachFinder. De transactiekosten van Mollie zelf (bijvoorbeeld voor iDEAL) betaal je aan Mollie.": "RaceCoachFinder charges a 2% service fee. The rider pays it on top of your invoice amount and it goes to RaceCoachFinder automatically. You pay Mollie's own transaction costs (for example for iDEAL) to Mollie.",
        "Nog niet beschikbaar": "Not available yet",
        "Online betalen wordt binnenkort aangezet. Dan kun je hier je Mollie-account koppelen.": "Online payments will be switched on soon. You can then connect your Mollie account here.",
        "Niet gekoppeld": "Not connected",
        "Koppel je Mollie-account om facturen te kunnen sturen. Heb je nog geen Mollie-account? Dan maak je er tijdens het koppelen gratis een aan.": "Connect your Mollie account to be able to send invoices. No Mollie account yet? You can create one for free while connecting.",
        "Gekoppeld": "Connected",
        "Rijders betalen je facturen rechtstreeks op dit Mollie-account.": "Riders pay your invoices directly into this Mollie account.",
        "Koppeling werkt niet meer": "Connection no longer works",
        "Mollie geeft geen toegang meer tot je account. Koppel je Mollie-account opnieuw.": "Mollie no longer gives access to your account. Please connect your Mollie account again.",
        "Gekoppeld, maar nog niet actief": "Connected, but not active yet",
        "Je Mollie-account kan nog geen betalingen ontvangen. Rond je aanmelding af in Mollie (bijvoorbeeld je bedrijfsgegevens en bankrekening). Daarna kun je facturen sturen.": "Your Mollie account cannot receive payments yet. Complete your sign-up in Mollie (for example your business details and bank account). After that you can send invoices.",
        "Status niet geladen": "Status not loaded",
        "Kon de status van je Mollie-koppeling niet laden. Probeer het later opnieuw.": "Could not load the status of your Mollie connection. Please try again later.",
        "Koppel je Mollie-account": "Connect your Mollie account",
        "Nog geen Mollie? Maak een account aan": "No Mollie yet? Create an account",
        "Status vernieuwen": "Refresh status",
        "Ontkoppelen": "Disconnect",
        "Opnieuw koppelen": "Connect again",
        "Naar Mollie": "Go to Mollie",
        "Naar Instellingen": "Go to Settings",
        "Testmodus: betalingen zijn nog niet echt.": "Test mode: payments are not real yet.",
        "Je Mollie-account is gekoppeld.": "Your Mollie account is connected.",
        "Koppelen met Mollie is geannuleerd.": "Connecting with Mollie was cancelled.",
        "Koppelen met Mollie is niet gelukt.": "Connecting with Mollie failed.",
        "Koppelen met Mollie is niet gelukt. Probeer het later opnieuw.": "Connecting with Mollie failed. Please try again later.",
        "De koppeling is verlopen. Probeer het opnieuw.": "The connection request has expired. Please try again.",
        "Mollie heeft de koppeling niet bevestigd. Probeer het opnieuw.": "Mollie did not confirm the connection. Please try again.",
        "Online betalen is nog niet beschikbaar op RaceCoachFinder. Probeer het later opnieuw.": "Online payment is not yet available on RaceCoachFinder. Please try again later.",
        "Koppel eerst je Mollie-account (Instellingen → Betalingen ontvangen). Daarna betalen rijders je facturen rechtstreeks aan jou.": "First connect your Mollie account (Settings → Receiving payments). Riders will then pay your invoices directly to you.",
        "De koppeling met je Mollie-account werkt niet meer. Koppel je Mollie-account opnieuw via Instellingen → Betalingen ontvangen.": "The connection with your Mollie account no longer works. Connect your Mollie account again via Settings → Receiving payments.",
        "Je Mollie-account is nog niet klaar om betalingen te ontvangen. Rond je aanmelding bij Mollie af; daarna kun je facturen sturen.": "Your Mollie account is not ready to receive payments yet. Complete your sign-up with Mollie; after that you can send invoices.",
        "Online betalen is op dit moment niet beschikbaar. Probeer het later opnieuw.": "Online payment is not available right now. Please try again later.",
        "Deze coach kan op dit moment nog geen online betalingen ontvangen. Neem contact op met je coach.": "This coach cannot receive online payments yet. Please contact your coach.",
        "Je betaalt rechtstreeks aan de coach, via Mollie. De servicekosten van 2% gaan naar RaceCoachFinder.": "You pay the coach directly, via Mollie. The 2% service fee goes to RaceCoachFinder.",
        "Dit bedrag is te laag om online te betalen.": "This amount is too low to pay online.",
        "Deze factuur is al betaald.": "This invoice has already been paid.",
        "Deze factuur staat niet meer open.": "This invoice is no longer open.",
        "Mollie-account ontkoppelen? Rijders kunnen je facturen dan niet meer online betalen, en je kunt geen nieuwe facturen sturen tot je opnieuw koppelt.": "Disconnect your Mollie account? Riders can then no longer pay your invoices online, and you cannot send new invoices until you connect again.",
    };

    // Teksten met een getal of naam erin (worden in de pagina zelf samengesteld)
    const PATRONEN = [
        [/^👥 Max (\d+) rijders?$/, '👥 Max $1 riders'],
        [/^👥 Al (\d+) rijders?$/, '👥 Already $1 riders'],
        [/^(\d+) beschikbaar$/, '$1 available'],
        [/^(\d+) coaches gevonden$/, '$1 coaches found'],
        [/^(\d+) aanvra(?:ag|gen)$/, '$1 requests'],
        [/^(\d+) verzoek(?:en)?$/, '$1 requests'],
        [/^Welkom, (.+)$/, 'Welcome, $1'],
        [/^Welkom terug, (.+)$/, 'Welcome back, $1'],
        [/^Nog niet betaald \((\d+)\)$/, 'Not yet paid ($1)'],
        [/^Alles \((\d+)\)$/, 'All ($1)'],
        [/^🎁 Gratis periode actief — verloopt op (.+)$/, '🎁 Free period active — expires on $1'],
        [/^Geplaatst (.+)$/, 'Posted $1'],

        // Automatische berichten in de chat (worden door de server in het Nederlands opgeslagen)
        [/^➕ (.+) heeft gereageerd op de aanvraag\.$/, '➕ $1 responded to the request.'],
        [/^👥 (.+) heeft een groepschat aangemaakt voor deze aanvraag\.$/, '👥 $1 created a group chat for this request.'],
        [/^(.+) heeft de training aangepast en nodigt je opnieuw uit: (.+)$/, '$1 changed the training and invites you again: $2'],
        [/^(.+) heeft jouw aandeel aangepast en nodigt je opnieuw uit voor: (.+)$/, '$1 changed your share and invites you again to: $2'],
        [/^(.+) nodigt je uit voor: (.+)$/, '$1 invites you to: $2'],
        [/^💰 Jouw aandeel: (.+)$/, '💰 Your share: $1'],
        [/^🚫 (.+) is geen coach meer bij deze training\.$/, '🚫 $1 is no longer a coach for this training.'],
        [/^ℹ️ (.+) heeft je uitnodiging voor "(.+)" ingetrokken\. Je hoeft niets meer te doen\.$/, 'ℹ️ $1 withdrew your invitation for "$2". You do not need to do anything.'],
        [/^✅ (.+) heeft de uitnodiging geaccepteerd\.$/, '✅ $1 accepted the invitation.'],
        [/^❌ (.+) heeft de uitnodiging voor "(.+)" geweigerd\. Je kan de training aanpassen zodat hij alsnog online kan komen\.$/, '❌ $1 declined the invitation for "$2". You can change the training so it can still go online.'],
        [/^❌ (.+) heeft de uitnodiging geweigerd\.$/, '❌ $1 declined the invitation.'],
        [/^👥 (.+) heeft de groep "(.+)" aangemaakt\.$/, '👥 $1 created the group "$2".'],
        [/^🚫 (.+) is uit de groep verwijderd\.$/, '🚫 $1 was removed from the group.'],
        [/^➕ (.+) is aan de groep toegevoegd\.$/, '➕ $1 was added to the group.'],
        [/^👋 (.+) heeft de groep verlaten\.$/, '👋 $1 left the group.'],
        [/^💶 Betaalverzoek verstuurd — (.+) — (.+) p\.p\.$/, '💶 Payment request sent — $1 — $2 per rider'],
        [/^📲 Elke (?:rijder|deelnemer) kan betalen via het persoonlijk gesprek\.$/, '📲 Every rider can pay via the private chat.'],
        [/^👋 (.+) heeft gereageerd op de activiteit\.$/, '👋 $1 responded to the activity.'],
        [/^✏️ (.+) heeft de training aangepast\. Bekijk hem via de knop 'Training' bovenaan\.$/, "✏️ $1 changed the training. View it with the 'Training' button at the top."],
        [/^➕ (.+) is als coach aan de training toegevoegd\.$/, '➕ $1 was added as a coach to the training.'],
        [/^➖ (.+) coacht niet meer mee met deze training\.$/, '➖ $1 is no longer coaching this training.'],
        [/^✅ (.+) is akkoord met de training\.$/, '✅ $1 agrees with the training.'],
        [/^↩️ (.+) is \(nog\) niet akkoord met de training\.$/, '↩️ $1 does not agree with the training (yet).'],
        [/^🎉 De training '(.+)' is gepubliceerd en staat nu bij Groepscoaching → Coach biedt aan\.$/, "🎉 The training '$1' has been published and is now listed under Group Coaching → Coach offers."],
        [/^🎯 Uitnodiging: (.+)$/, '🎯 Invitation: $1'],
        [/^Groepsgesprek · (.+)$/, 'Group chat · $1'],
        [/^👥 (\d+)\/(\d+) rijders(?: \(vol\))?$/, '👥 $1/$2 riders'],
        [/^(\d+)\/(\d+) rijders \(vol\)$/, '$1/$2 riders (full)'],
        [/^(\d+)\/(\d+) rijders$/, '$1/$2 riders'],
        [/^Fout bij starten betaling: (.+)$/, 'Could not start payment: $1'],
    ];

    // ============================================================
    //  ENGINE
    // ============================================================
    const I18N = {
        taal: localStorage.getItem(TAAL_KEY) || 'nl',

        /** Vertaal een sleutel – bruikbaar in JS-templates */
        t(sleutel) {
            if (this.taal === 'en' && EN[sleutel] !== undefined) return EN[sleutel];
            return sleutel;
        },

        setTaal(taal) {
            this.taal = taal;
            localStorage.setItem(TAAL_KEY, taal);
            this._hertaalDOM(document.body);
            this._updateToggle();
        },

        _hertaalDOM(root) {
            if (!root || !root.nodeType) return;
            const taal = this.taal;

            // --- Tekstknooppunten ---
            const walker = document.createTreeWalker(
                root,
                NodeFilter.SHOW_TEXT,
                {
                    acceptNode(node) {
                        const p = node.parentElement;
                        if (!p) return NodeFilter.FILTER_REJECT;
                        const tag = p.tagName;
                        if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'TEXTAREA') return NodeFilter.FILTER_REJECT;
                        if (!node.textContent.trim()) return NodeFilter.FILTER_REJECT;
                        return NodeFilter.FILTER_ACCEPT;
                    }
                }
            );

            const nodes = [];
            let n;
            while ((n = walker.nextNode())) nodes.push(n);

            for (const node of nodes) {
                if (!originals.has(node)) originals.set(node, node.textContent);
                const orig = originals.get(node);
                const key = orig.trim();
                if (taal === 'en' && EN[key] !== undefined) {
                    node.textContent = orig.replace(key, EN[key]);
                } else if (taal === 'en' && PATRONEN.some(([re]) => re.test(key))) {
                    const [re, vervang] = PATRONEN.find(([r]) => r.test(key));
                    node.textContent = orig.replace(key, key.replace(re, vervang));
                } else if (taal === 'nl') {
                    node.textContent = orig;
                }
            }

            if (!root.querySelectorAll) return;

            // --- Placeholder-attributen ---
            root.querySelectorAll('[placeholder]').forEach(el => {
                if (!el._i18nPh) el._i18nPh = el.getAttribute('placeholder');
                const orig = el._i18nPh;
                if (taal === 'en' && EN[orig]) el.setAttribute('placeholder', EN[orig]);
                else el.setAttribute('placeholder', orig);
            });

            // --- Title-attributen (tooltips) ---
            root.querySelectorAll('[title]').forEach(el => {
                if (!el._i18nTitle) el._i18nTitle = el.getAttribute('title');
                const orig = el._i18nTitle;
                if (taal === 'en' && EN[orig]) el.setAttribute('title', EN[orig]);
                else el.setAttribute('title', orig);
            });
        },

        _updateToggle() {
            const taal = this.taal;
            document.querySelectorAll('#taal-nl-btn').forEach(b => b.classList.toggle('actief', taal === 'nl'));
            document.querySelectorAll('#taal-en-btn').forEach(b => b.classList.toggle('actief', taal === 'en'));
        },

        _startObserver() {
            const self = this;
            const mo = new MutationObserver(mutations => {
                if (self.taal === 'nl') return;
                for (const m of mutations) {
                    for (const node of m.addedNodes) {
                        if (node.nodeType === Node.ELEMENT_NODE) self._hertaalDOM(node);
                        // ook tekst die met el.textContent = '...' wordt gezet (nieuwe tekstknoop)
                        else if (node.nodeType === Node.TEXT_NODE && node.parentElement) self._hertaalDOM(node.parentElement);
                    }
                }
            });
            mo.observe(document.body, { childList: true, subtree: true });
        },

        _injectStyles() {
            const s = document.createElement('style');
            s.textContent =
                '.taal-toggle{display:flex;border:1.5px solid rgba(0,0,0,0.13);border-radius:20px;overflow:hidden}' +
                '.taal-optie{padding:0.17rem 0.58rem;background:none;border:none;font-size:0.75rem;font-weight:700;' +
                'cursor:pointer;color:var(--kleur-subtekst,#888);font-family:inherit;transition:all 0.15s;line-height:1.7}' +
                '.taal-optie.actief{background:var(--kleur-primair,#F5C200);color:var(--kleur-donker,#1a1a1a)}' +
                '.taal-optie:hover:not(.actief){color:var(--kleur-tekst,#1a1a1a);background:rgba(0,0,0,0.05)}';
            document.head.appendChild(s);
        },

        init() {
            this._injectStyles();
            const run = () => {
                this._hertaalDOM(document.body);
                this._updateToggle();
                this._startObserver();
            };
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', run);
            } else {
                run();
            }
        }
    };

    window.I18N = I18N;
    I18N.init();
})();
