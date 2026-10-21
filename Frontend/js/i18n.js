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
        'Groepstraining': 'Group Training',
        'Persoonlijk coachen': 'Personal Coaching',
        'Berichten': 'Messages',
        'Inloggen': 'Log in',
        'Registreren': 'Register',
        'Uitloggen': 'Log out',
        'Instellingen': 'Settings',

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
        'kartcoach': 'kart coach',
        'Eén platform voor rijders en coaches: vind een coach, regel persoonlijke begeleiding of doe mee aan een groepstraining. Alles via chat.': 'One platform for riders and coaches: find a coach, arrange personal coaching or join a group training. All through chat.',
        'Word coach': 'Become a coach',
        'Groepstrainingen': 'Group trainings',
        'Open aanvragen': 'Open requests',
        'Welkom terug,': 'Welcome back,',
        'Snel naar:': 'Quick links:',
        'Wat wil je doen?': 'What would you like to do?',
        'Coach zoeken': 'Find a coach',
        'Bekijk profielen, ervaring, prijzen en reviews van kartcoaches en neem direct contact op.': 'View profiles, experience, prices and reviews of kart coaches and get in touch directly.',
        'Naar coaches': 'Go to coaches',
        'Rijders plaatsen een aanvraag voor één-op-één begeleiding. Coaches bieden aan wanneer en waar ze beschikbaar zijn.': 'Riders post a request for one-to-one coaching. Coaches offer when and where they are available.',
        'Naar persoonlijk coachen': 'Go to personal coaching',
        'Doe mee met een training, wedstrijddag of evenement met meerdere rijders, soms met meerdere coaches.': 'Join a training, race day or event with several riders, sometimes with several coaches.',
        'Naar groepstrainingen': 'Go to group trainings',
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
        'Zoek en vergelijk coaches op locatie, discipline en prijs': 'Search and compare coaches by location, discipline and price',
        'Plaats een aanvraag als je een coach zoekt voor een training of wedstrijd': 'Post a request when you need a coach for a training or race',
        'Sluit je aan bij groepstrainingen': 'Join group trainings',
        'Chat met coaches en betaal facturen veilig online': 'Chat with coaches and pay invoices securely online',
        'Laat na je coaching een review achter': 'Leave a review after your coaching',
        'Gratis voor rijders': 'Free for riders',
        'Gratis account maken': 'Create a free account',
        'Maak een profiel met je ervaring, specialisaties en prijzen': 'Create a profile with your experience, specialisations and prices',
        'Reageer op rijders die een coach zoeken': 'Respond to riders looking for a coach',
        'Organiseer groepstrainingen, ook samen met andere coaches': 'Organise group trainings, also together with other coaches',
        'Stuur betaalverzoeken en facturen vanuit de chat': 'Send payment requests and invoices from the chat',
        'Houd je afspraken bij in je eigen agenda': 'Keep track of your appointments in your own calendar',
        '€10 per maand · maandelijks opzegbaar': '€10 per month · cancel monthly',
        'Eerste maand €5, daarna €10 per maand · maandelijks opzegbaar': 'First month €5, then €10 per month · cancel monthly',
        'Komende groepstrainingen': 'Upcoming group trainings',
        'Alle groepstrainingen': 'All group trainings',
        'Aanbevolen voor jou': 'Recommended for you',
        'Coaches die passen bij jouw specialisaties en niveau': 'Coaches that match your specialisations and level',
        'Alle coaches bekijken': 'View all coaches',
        'Recente coaches': 'Recent coaches',
        'Coaches laden...': 'Loading coaches…',
        'Zo werkt het': 'How it works',
        'Maak een account': 'Create an account',
        'Kies of je rijder of coach bent en vul je profiel in.': 'Choose whether you are a rider or coach and fill in your profile.',
        'Vind elkaar': 'Find each other',
        'Zoek een coach, plaats een aanvraag of meld je aan voor een groepstraining.': 'Find a coach, post a request or sign up for a group training.',
        'Regel alles via chat': 'Arrange everything through chat',
        'Spreek af, ontvang de factuur en betaal veilig online.': 'Make arrangements, receive the invoice and pay securely online.',
        'Meer uitleg over hoe het werkt': 'More about how it works',
        'Naar mijn dashboard': 'Go to my dashboard',
        '💬 Berichten': '💬 Messages',
        '👥 Training aanmaken': '👥 Create training',
        '🎯 Rijders zoeken een coach': '🎯 Riders looking for a coach',
        'Rijders zoeken een coach': 'Riders looking for a coach',
        '🎯 Aanvraag plaatsen': '🎯 Post a request',
        '👥 Groepstrainingen': '👥 Group trainings',
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
        '👥 Groepstraining': '👥 Group Training',
        'Coaches die groepssessies aanbieden en rijders die een groep zoeken.': 'Coaches offering group sessions and riders looking for a group.',
        '🏁 Coach biedt groep aan': '🏁 Coach offers group',
        '🙋 Rijder zoekt groep': '🙋 Rider seeks group',
        'Coach biedt groep aan': 'Coach offers group',
        'Rijder zoekt groep': 'Rider seeks group',
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
        'Zoek een groepstraining': 'Find a group training',
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
    };

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
