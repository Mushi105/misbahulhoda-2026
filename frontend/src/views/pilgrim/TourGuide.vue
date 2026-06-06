<script setup>
import { ref, onMounted, computed } from 'vue'
import api from '@/api/client'

const activeTab = ref('schedule')
const expandedDay = ref(null)
const scholarsFromApi = ref([])
const scholarsLoading = ref(false)

const avatarColors = [
  'from-primary-700 to-primary-900',
  'from-gold-700 to-gold-900',
  'from-red-800 to-red-950',
  'from-purple-800 to-purple-950',
  'from-teal-700 to-teal-900',
  'from-blue-700 to-blue-900',
]

const flagMap = {
  'United States': '🇺🇸', 'United Kingdom': '🇬🇧', 'Canada': '🇨🇦',
  'Australia': '🇦🇺', 'Pakistan': '🇵🇰', 'India': '🇮🇳',
  'Bahrain': '🇧🇭', 'Iraq': '🇮🇶', 'Iran': '🇮🇷', 'UAE': '🇦🇪',
}

function mapScholar(s, i) {
  const initials = s.fullName.split(' ').map(n => n[0]).slice(0, 2).join('')
  return {
    id: s.id,
    name: s.fullName,
    lang: s.language,
    initials,
    avatarColor: avatarColors[i % avatarColors.length],
    photoUrl: s.photoUrl || null,
    location: s.location,
    flag: flagMap[s.location] || '🌍',
    specialty: s.specialty,
    tags: s.tags ? s.tags.split(',').map(t => t.trim()).filter(Boolean) : [],
    quote: s.quote || null,
    bio: s.bio,
    youtube: s.youtubeSearch || null,
    youtubeUrl: s.youtubeUrl || null,
    website: s.website || null,
    isReciter: s.isReciter,
  }
}

const dynamicScholars = computed(() => scholarsFromApi.value.filter(s => !s.isReciter))
const dynamicReciters = computed(() => scholarsFromApi.value.filter(s => s.isReciter))
const useApi = computed(() => scholarsFromApi.value.length > 0)

async function loadScholars() {
  scholarsLoading.value = true
  try {
    const res = await api.get('/scholars')
    const data = res.data.data || []
    scholarsFromApi.value = data.map((s, i) => mapScholar(s, i))
  } catch { /* fallback to hardcoded */ }
  finally { scholarsLoading.value = false }
}

onMounted(() => { loadScholars(); loadHotels() })

const tabs = [
  { id: 'schedule', label: 'Schedule', icon: '📅' },
  { id: 'hotels', label: 'Hotels', icon: '🏨' },
  { id: 'scholars', label: 'Scholars', icon: '📚' },
  { id: 'walk', label: 'Arbaeen Walk', icon: '🚶' },
  { id: 'maps', label: 'Holy Sites', icon: '🕌' },
  { id: 'team', label: 'Our Team', icon: '👥' },
]

const itinerary = [
  {
    day: 1, date: 'Sunday, 2 Aug 2026', city: 'Karachi → Kadhimiyyah',
    theme: 'Departure Day',
    events: [
      { time: 'Morning', activity: 'Depart from Karachi, Pakistan' },
      { time: 'Afternoon', activity: 'Arrival in Baghdad, Iraq' },
      { time: 'Evening', activity: 'Transfer to Kadhimiyyah — Hotel Check-in' },
      { time: 'Night', activity: 'Rest & settle in. Ziyarat of Imam Kadhim (AS) & Imam Jawad (AS) — optional' },
    ],
    notes: 'Keep your passport & visa documents easily accessible. Dress modestly.'
  },
  {
    day: 2, date: 'Monday, 3 Aug 2026', city: 'Kadhimiyyah',
    theme: 'Ziyarat of Kadhimain',
    events: [
      { time: 'Fajr', activity: 'Namaz-e-Fajr + Ziyarat of Imam Kadhim (AS)' },
      { time: 'Morning', activity: 'Guided tour of Kadhimiyyah Haram' },
      { time: 'Afternoon', activity: 'Rest & lunch at hotel' },
      { time: 'Evening', activity: 'Majalis — Speaker TBC' },
      { time: 'Night', activity: 'Dua Kumail & Noha program' },
    ],
    notes: 'Separate entrances for men and women at the Haram.'
  },
  {
    day: 3, date: 'Tuesday, 4 Aug 2026', city: 'Kadhimiyyah → Samarra',
    theme: 'Samarra Ziyarat',
    events: [
      { time: 'Morning', activity: 'After Fajr — depart for Samarra by bus' },
      { time: 'Midday', activity: 'Arrive Samarra — Ziyarat of Imam Ali Hadi (AS) & Imam Hasan Askari (AS)' },
      { time: 'Afternoon', activity: 'Ziyarat of Sahlaat al-Askari (AS)' },
      { time: 'Evening', activity: 'Return to Kadhimiyyah hotel' },
      { time: 'Night', activity: 'Majalis & Noha program' },
    ],
    notes: 'Full day trip. Bring water & snacks. Dress appropriately for holy shrine.'
  },
  {
    day: 4, date: 'Wednesday, 5 Aug 2026', city: 'Kadhimiyyah → Najaf',
    theme: 'Travel to Najaf',
    events: [
      { time: 'Morning', activity: 'Check out from Kadhimiyyah hotel after Fajr' },
      { time: 'Midday', activity: 'Arrive Najaf — Hotel Check-in (Hotel Luluat Al-Bahar)' },
      { time: 'Afternoon', activity: 'Rest & Zuhr/Asr prayers' },
      { time: 'Evening', activity: 'First Ziyarat of Imam Ali (AS) at Haram' },
      { time: 'Night', activity: 'Wadi-us-Salaam visit — cemetery of Prophets & believers' },
    ],
    notes: 'Wadi-us-Salaam is the largest cemetery in the world. Recite Surah Yasin for the departed.'
  },
  {
    day: 5, date: 'Thursday, 6 Aug 2026', city: 'Najaf',
    theme: 'Jumma & Masajid Ziyarat',
    events: [
      { time: 'Fajr', activity: 'Namaz-e-Fajr at Haram of Imam Ali (AS)' },
      { time: 'Morning', activity: 'Masjid Kufa — Ziyarat & 2 Rakat prayer at Mihrabs' },
      { time: 'Midday', activity: 'Masjid Sahla — special Ziyarat (Maqam of Imam Mahdi AJTF)' },
      { time: 'Afternoon', activity: 'Jumma Prayers' },
      { time: 'Evening', activity: 'Majalis — English session' },
      { time: 'Night', activity: 'Noha & Marsiya program' },
    ],
    notes: 'Masjid Sahla has Maqam Imam Mahdi (AJTF) — spend time in dua and Munajat.'
  },
  {
    day: 6, date: 'Friday, 7 Aug 2026', city: 'Najaf',
    theme: 'Deep Ziyarat Day',
    events: [
      { time: 'Fajr', activity: 'Haram Ziyarat of Amir ul Momineen (AS)' },
      { time: 'Morning', activity: 'Visit Masjid Hannan — historical mosque near Najaf' },
      { time: 'Afternoon', activity: 'Ziyarat of Muslim ibn Aqeel (RA) — Kufa' },
      { time: 'Evening', activity: 'Majalis — Urdu session' },
      { time: 'Night', activity: 'Dua Tawassul & prayers at Haram' },
    ],
    notes: 'Muslim ibn Aqeel (RA) was the representative of Imam Hussain (AS) in Kufa.'
  },
  {
    day: 7, date: 'Saturday, 8 Aug 2026', city: 'Najaf → Karbala (Walk Begins)',
    theme: 'Start of Arbaeen Walk 🚶',
    events: [
      { time: 'After Fajr', activity: 'Final Ziyarat of Imam Ali (AS) & farewell' },
      { time: 'Morning', activity: 'Walk begins — Pole 1 checkpoint from Najaf' },
      { time: 'Throughout Day', activity: 'Walking the sacred path — 80km journey begins' },
      { time: 'Evening', activity: 'Reach night camp — Mawkab hospitality (food & water provided)' },
      { time: 'Night', activity: 'Rest at Mawkab. Majalis & Noha' },
    ],
    notes: 'This is the greatest pilgrimage walk in history. Walk with sincerity. Mawkabs serve FREE food to all pilgrims.'
  },
  {
    day: 8, date: 'Sunday, 9 Aug 2026', city: 'Walk — Day 2',
    theme: 'Arbaeen Walk Continues',
    events: [
      { time: 'Early morning', activity: 'Fajr prayers at Mawkab & continue walking' },
      { time: 'Morning', activity: 'Pass through villages — Mawkabs everywhere' },
      { time: 'Afternoon', activity: 'Midpoint rest & meals' },
      { time: 'Evening', activity: 'Night Mawkab camp' },
      { time: 'Night', activity: 'Ziyarat recitations, Latmiyya & Noha' },
    ],
    notes: 'Wear comfortable shoes. Walk at your own pace. Group leaders will track via poles.'
  },
  {
    day: 9, date: 'Monday, 10 Aug 2026', city: 'Walk — Day 3 → Karbala',
    theme: 'Arrival in Karbala',
    events: [
      { time: 'Morning', activity: 'Final stretch toward Karbala' },
      { time: 'Afternoon', activity: 'Enter Karbala — approach Haram from main Ziyarat road' },
      { time: 'Evening', activity: 'First Ziyarat of Imam Hussain (AS) — with tears & love' },
      { time: 'Night', activity: 'Hotel Check-in (Hotel Al-Jaber, Al Najareen Street)' },
    ],
    notes: 'The moment you see the golden dome of Imam Hussain (AS), recite Ziyarat Arbaeen.'
  },
  {
    day: 10, date: 'Tuesday, 11 Aug 2026', city: 'Karbala — Arbaeen Day',
    theme: '🌟 ARBAEEN — 40th Day of Imam Hussain (AS)',
    events: [
      { time: 'All Day', activity: 'Arbaeen Day — millions of pilgrims converge on Karbala' },
      { time: 'Fajr', activity: 'Namaz-e-Fajr at Haram of Imam Hussain (AS)' },
      { time: 'Morning', activity: 'Ziyarat of Hazrat Abbas (AS) — Alamdar of Karbala' },
      { time: 'Afternoon', activity: 'Majlis-e-Aza — commemorating the 40th day' },
      { time: 'Evening', activity: 'Main Arbaeen Majalis — large gathering' },
      { time: 'Night', activity: 'Stay near Haram — continuous Noha & Latmiyya' },
    ],
    notes: '🌹 This is the main day. Over 20 million pilgrims gather. Recite full Ziyarat Arbaeen at the Haram.'
  },
  {
    day: 11, date: 'Wednesday, 12 Aug 2026', city: 'Karbala',
    theme: 'Karbala Ziyarat & Reflection',
    events: [
      { time: 'Fajr', activity: 'Haram Ziyarat' },
      { time: 'Morning', activity: 'Visit Tented Maqams — sites of the martyrs of Karbala' },
      { time: 'Afternoon', activity: 'Rest & personal Ziyarat time' },
      { time: 'Evening', activity: 'Majalis — English session' },
      { time: 'Night', activity: 'Final night program in Karbala' },
    ],
    notes: 'Pakistan Independence Day (14 August). Carry water; the Haram can be very crowded.'
  },
  {
    day: 12, date: 'Thursday, 13 Aug 2026', city: 'Karbala',
    theme: 'Final Karbala Day',
    events: [
      { time: 'Fajr', activity: 'Namaz at Haram' },
      { time: 'Morning', activity: 'Farewell Ziyarat of Imam Hussain (AS) & Hazrat Abbas (AS)' },
      { time: 'Afternoon', activity: 'Shopping & resting' },
      { time: 'Evening', activity: 'Closing Majalis for the tour group' },
      { time: 'Night', activity: 'Packing & preparation for departure' },
    ],
    notes: 'Buy zamzam water (Imam Hussain AS Haram), turbah, and tasbih as gifts.'
  },
  {
    day: 13, date: 'Friday, 14 Aug 2026', city: 'Karbala → Baghdad',
    theme: 'Return to Baghdad',
    events: [
      { time: 'Morning', activity: 'Check out — depart for Baghdad airport' },
      { time: 'Afternoon', activity: 'Baghdad transit time' },
      { time: 'Evening', activity: 'Airport departure procedures' },
      { time: 'Night', activity: 'Depart Baghdad for Karachi' },
    ],
    notes: 'Keep all documents ready. Arrive at airport 3+ hours before departure.'
  },
  {
    day: 14, date: 'Saturday, 15 Aug 2026', city: 'Arrival Karachi',
    theme: 'Homecoming — Alhamdulillah',
    events: [
      { time: 'Morning', activity: 'Arrive Karachi — Alhamdulillah! Ziyarat complete' },
      { time: 'Afternoon', activity: 'Customs & immigration clearance' },
      { time: 'Evening', activity: 'Reunite with family' },
    ],
    notes: 'May Allah (SWT) accept your Ziyarat and pilgrimage. Amin!'
  },
]

const hotelsFromApi = ref([])
const hotelsLoading = ref(false)

const hotels = computed(() =>
  hotelsFromApi.value.map(h => ({
    ...h,
    // normalize field names for template
    lat: h.latitude,
    lng: h.longitude,
    phone: h.phoneNumber,
    haramLat: h.haramLatitude,
    haramLng: h.haramLongitude,
    color: h.colorClass || 'border-gray-300',
    icon: h.iconEmoji || '🏨',
    nearHaram: h.nearHaram || '',
    haramDistance: h.haramDistanceText || '',
    amenities: h.amenities ? h.amenities.split(',').map(a => a.trim()) : [],
    tips: h.tips || '',
    nights: h.nightsLabel || '',
    jamat: (h.jamatFajr || h.jamatZuhr) ? [
      { salah: 'Fajr',    adhan: h.adhanFajr,    jamat: h.jamatFajr },
      { salah: 'Zuhr',    adhan: h.adhanZuhr,    jamat: h.jamatZuhr },
      { salah: 'Asr',     adhan: h.adhanAsr,     jamat: h.jamatAsr },
      { salah: 'Maghrib', adhan: h.adhanMaghrib, jamat: h.jamatMaghrib },
      { salah: 'Isha',    adhan: h.adhanIsha,    jamat: h.jamatIsha },
    ].filter(s => s.jamat) : [],
  }))
)

async function loadHotels() {
  hotelsLoading.value = true
  try {
    const res = await api.get('/accommodation/hotels')
    hotelsFromApi.value = res.data?.data ?? []
  } catch {
    hotelsFromApi.value = []
  } finally {
    hotelsLoading.value = false
  }
}

const scholars = [
  {
    name: 'Shaykh Usama Abdulghani',
    lang: 'English',
    initials: 'UA',
    avatarColor: 'from-primary-700 to-primary-900',
    location: 'United States',
    flag: '🇺🇸',
    specialty: 'Contemporary Islamic Philosophy & Ethics',
    tags: ['YouTube Lectures', 'English Podcast', 'Quran Tafsir', 'Western Muslims'],
    quote: '"The walk to Hussain is not just with your feet — it is with your heart, your conscience, and your soul."',
    bio: 'Shaykh Usama Abdulghani is one of the most recognised English-speaking Islamic scholars in North America. Born and raised in the United States, he studied classical Islamic sciences in Qom, Iran, for over a decade before returning to serve the Muslim community in the West. He is deeply respected for his ability to address the spiritual and ethical questions that Muslims living in Western societies face, drawing on the rich tradition of the Ahlul Bayt (AS). Shaykh Usama has led Arbaeen pilgrimages for over 15 years and his lectures on the philosophy of Karbala are among the most watched Islamic lectures in the English language. His work focuses on connecting Western-born Muslims to their authentic roots while addressing contemporary challenges of identity, justice, and faith.',
    youtube: 'Search: Shaykh Usama Abdulghani',
  },
  {
    name: 'Sayyid Hassan Al-Ridha',
    lang: 'English',
    initials: 'SH',
    avatarColor: 'from-gold-700 to-gold-900',
    location: 'United Kingdom',
    flag: '🇬🇧',
    specialty: 'Quranic Sciences & Ahlul Bayt Traditions',
    tags: ['Quran Commentary', 'UK Based', 'Youth Programs', 'Academic Scholar'],
    quote: '"Every verse of the Quran leads you back to the household of the Prophet. Karbala is where the Quran was given its voice."',
    bio: 'Sayyid Hassan is a UK-based scholar who has dedicated his life to making the teachings of the Ahlul Bayt accessible to English-speaking Muslims across Europe, North America, and Australia. He holds formal qualifications in both classical Islamic sciences and Western academia, giving his lectures a unique depth and accessibility. Sayyid Hassan is particularly beloved among young Western Muslims for his ability to answer difficult questions about faith, purpose, and spirituality in a language that resonates with their lived experiences. His majalis on the events of Karbala are emotionally powerful while remaining intellectually rigorous. He has spoken at major Islamic centres in London, Toronto, Houston, and Sydney.',
    youtube: 'Search: Sayyid Hassan Al-Ridha Lectures',
  },
  {
    name: 'Sayyid Ali Zaidi',
    lang: 'English',
    initials: 'AZ',
    avatarColor: 'from-red-800 to-red-950',
    location: 'Canada',
    flag: '🇨🇦',
    specialty: 'History of Karbala & Imam Hussain (AS)',
    tags: ['History & Research', 'Canada Based', 'Academic Approach', 'Ashura Lectures'],
    quote: '"Hussain did not just die for Islam. He died to show humanity what it means to stand for truth when the whole world stands against you."',
    bio: 'Sayyid Ali Zaidi is a Canadian scholar of Pakistani and Iraqi heritage whose research into the events of Karbala has brought him international recognition. He combines deep traditional scholarship with historical academic methodology to present the events of Ashura in a way that is both emotionally moving and intellectually compelling. His annual Muharram lecture series is followed by thousands across Canada, the UK, and the USA. Sayyid Ali is particularly gifted at speaking to non-Muslims and new Muslims, explaining why Karbala is one of the most significant events in human history regardless of religious affiliation. His work makes the message of Imam Hussain (AS) accessible to anyone who values truth, justice, and human dignity.',
    youtube: 'Search: Sayyid Ali Zaidi Karbala',
  },
  {
    name: 'Shaykh Fadhl Abbas',
    lang: 'English',
    initials: 'FA',
    avatarColor: 'from-slate-600 to-slate-800',
    location: 'Bahrain / Global',
    flag: '🌍',
    specialty: 'Islamic Jurisprudence & Spiritual Development',
    tags: ['Fiqh & Law', 'Character Building', 'Globally Recognised', 'Noha Recitation'],
    quote: '"The lesson of Karbala is not to mourn — it is to transform. Let Hussain change how you live, not just how you feel."',
    bio: 'Shaykh Fadhl Abbas is a globally recognised Islamic scholar and poet known equally for his profound English-language lectures and his deeply moving noha recitations. Trained in classical Islamic jurisprudence, he has become one of the most influential voices in the English-speaking Shia Muslim world. His lectures combine practical Islamic guidance — on prayer, family life, character, and ethics — with the overarching spiritual message of Imam Hussain (AS). Shaykh Fadhl has spoken at events in Bahrain, the UK, USA, Canada, Australia, and across the Gulf, making him one of the most widely-travelled and recognised scholars of his generation. His work is particularly valuable for families seeking to raise children connected to their faith in a Western environment.',
    youtube: 'Search: Shaykh Fadhl Abbas',
  },
  {
    name: 'Syed Nusrat Bukhari',
    lang: 'Urdu',
    initials: 'NB',
    avatarColor: 'from-green-800 to-green-950',
    location: 'Pakistan',
    flag: '🇵🇰',
    specialty: 'Urdu Majalis & Classical Khitabat',
    tags: ['Urdu Lectures', 'Pakistan', 'Classical Style', '30+ Years Experience'],
    quote: '"جب زبان سے حسین کا نام لیتے ہیں تو دل خود بخود جھک جاتا ہے۔"',
    bio: 'Syed Nusrat Bukhari is one of the most revered Urdu-language Zakirs of his generation, with over 30 years of experience delivering Majalis across Pakistan, the Gulf, and the global Pakistani diaspora. His deep voice, classical style, and mastery of Urdu poetry make his Majalis an experience that touches the deepest part of the soul. Syed Nusrat is known for his encyclopaedic knowledge of the events of Karbala and his ability to bring those events to life through powerful oratory. He is particularly beloved by older generations and by those who find spiritual connection through the Urdu classical tradition of Khitabat. His Majalis during this tour will be in Urdu and are suitable for all Urdu-speaking pilgrims.',
    youtube: 'Search: Syed Nusrat Bukhari Majalis',
  },
  {
    name: 'Syed Zameer Jaffri',
    lang: 'Urdu',
    initials: 'ZJ',
    avatarColor: 'from-purple-800 to-purple-950',
    location: 'Pakistan',
    flag: '🇵🇰',
    specialty: 'Urdu Poetry, Marsiya & Literary Scholarship',
    tags: ['Urdu Poetry', 'Marsiya', 'Literary Scholar', 'Award Winning'],
    quote: '"کربلا صرف تاریخ نہیں، کربلا ہر اس شخص کی کہانی ہے جس نے کبھی باطل کے سامنے سر نہیں جھکایا۔"',
    bio: 'Syed Zameer Jaffri stands in a league of his own as both a scholar and a literary giant of the Urdu language. An award-winning Urdu poet, his Marsiya recitations are widely considered to be among the finest in the contemporary world, carrying on a centuries-old tradition of commemorating the tragedy of Karbala through verse. His deep scholarship in classical Urdu and Persian literature gives his recitations a beauty and depth that moves even those who do not fully understand every word. Syed Zameer has performed at the world\'s most prestigious Majalis gatherings and his work has been published in several acclaimed poetry collections. His presence on this tour is a rare privilege for all pilgrims.',
    youtube: 'Search: Syed Zameer Jaffri Marsiya',
  },
]

const reciters = [
  {
    name: 'Sayed Zaigham Ali Shan',
    lang: 'English',
    initials: 'ZS',
    avatarColor: 'from-blue-700 to-blue-900',
    flag: '🇬🇧',
    specialty: 'English Noha & Latmiyya',
    note: 'One of the most powerful voices in English-language Noha. His recitations have introduced the message of Karbala to thousands of non-Urdu speaking Muslims across the UK, USA, Canada, and Australia. Known for moving entire congregations to tears through the beauty and sincerity of his voice.',
    tags: ['English Noha', 'UK', 'International', 'Youth Favourite'],
  },
  {
    name: 'Sayed Raza Haider',
    lang: 'Urdu',
    initials: 'RH',
    avatarColor: 'from-emerald-700 to-emerald-900',
    flag: '🇵🇰',
    specialty: 'Urdu Noha & Soz',
    note: 'A soulful and deeply emotive voice in Urdu Noha Khaani. Sayed Raza Haider\'s recitations carry the pain of Karbala with a sincerity that has moved millions. His Nohas have been listened to by Pakistani communities worldwide and he is considered one of the rising voices of the next generation.',
    tags: ['Urdu Noha', 'Pakistan', 'Soz', 'Emotive Style'],
  },
  {
    name: 'Syed Murtaza Zaidi',
    lang: 'Urdu',
    initials: 'MZ',
    avatarColor: 'from-rose-700 to-rose-900',
    flag: '🇵🇰',
    specialty: 'Marsiya & Classical Noha',
    note: 'Coming from a long lineage of reciters, Syed Murtaza brings depth, authenticity, and classical training to every recitation. His Marsiya recitations honour the centuries-old tradition of commemorating the martyrs of Karbala through classical Urdu verse, making each session a profound spiritual experience.',
    tags: ['Classical Marsiya', 'Pakistan', 'Heritage Style', 'Lineage Scholar'],
  },
]

const khuddamTeam = [
  { name: 'Jameel Hyder', location: 'USA' },
  { name: 'Syed Wajahat Ali', location: 'UK' },
  { name: 'Faheem Naqvi', location: 'UK' },
  { name: 'Zaheer Abbas', location: 'UK' },
  { name: 'Asif Raza', location: 'UK' },
  { name: 'Hasan Kazmi', location: 'UK' },
  { name: 'Aon Naqvi', location: 'UK' },
  { name: 'Zakria Shafi', location: 'UK' },
  { name: 'Kamraan Rizvi', location: 'UK' },
  { name: 'Muzammil Hussein', location: 'Canada' },
  { name: 'Syed Qaiser', location: 'UK' },
  { name: 'Wali Haider', location: 'UK' },
  { name: 'Danyal Abbas', location: 'UK' },
  { name: 'Ali Raza', location: 'UK' },
]

const holySites = [
  {
    name: 'Haram of Imam Kadhim (AS) & Imam Jawad (AS)',
    city: 'Kadhimiyyah, Baghdad',
    icon: '🕌',
    description: 'The sacred shrine of the 7th and 9th Imams. The golden minarets are visible from across Baghdad.',
    ziyarat: 'Ziyarat Imam Kadhim (AS) recited at the shrine. Best time: after Fajr or after Maghrib.',
    tip: 'Separate queues for men and women. Remove shoes before entering the inner sanctum.'
  },
  {
    name: 'Haram of Imam Ali (AS)',
    city: 'Najaf Al Ashraf',
    icon: '⚜️',
    description: 'The greatest shrine of the Commander of the Faithful, first Imam and cousin & son-in-law of the Prophet (SAWW).',
    ziyarat: 'Ziyarat Aminallah is recommended at this shrine. Night Ziyarat is especially powerful.',
    tip: 'Wadi-us-Salaam cemetery surrounds the shrine. Many Prophets & companions are buried here.'
  },
  {
    name: 'Masjid Kufa',
    city: 'Kufa, near Najaf',
    icon: '🕍',
    description: 'One of the oldest and most historic mosques in Islam. Imam Ali (AS) was martyred here. Has multiple sacred mihrabs.',
    ziyarat: 'Offer 2 Rakat at each of the 7 sacred mihrabs inside the mosque.',
    tip: 'The spot where Imam Ali (AS) was struck is marked. Recite Dua at this location.'
  },
  {
    name: 'Masjid Sahla',
    city: 'Kufa, near Najaf',
    icon: '✨',
    description: 'Believed to be the future home of Imam Mahdi (AJTF). Has Maqams of many Prophets. A place of answered prayers.',
    ziyarat: 'Special Dua for the reappearance of Imam Mahdi (AJTF). Dua Faraj is recommended.',
    tip: 'Masjid Sahla has a special spiritual atmosphere. Many pilgrims report powerful spiritual experiences here.'
  },
  {
    name: 'Haram of Imam Hussain (AS)',
    city: 'Karbala Al Muqaddasa',
    icon: '🌹',
    description: 'The most visited pilgrimage site in the world after Makkah. Imam Hussain (AS) — grandson of the Prophet — is buried here.',
    ziyarat: 'Ziyarat Arbaeen is specifically recommended on Arbaeen day. Ziyarat Ashura is available in most books.',
    tip: 'The Haram is open 24 hours. Night visits are deeply spiritual. Approach with Ziyarat recitation from the outer gate.'
  },
  {
    name: 'Haram of Hazrat Abbas (AS)',
    city: 'Karbala Al Muqaddasa',
    icon: '🏴',
    description: 'Shrine of the Alamdar (flag-bearer) of Karbala, the loyal brother of Imam Hussain (AS). Known for answered duas.',
    ziyarat: 'Ziyarat of Abbas ibn Ali. Special dua for those seeking shifa (healing) and relief from difficulties.',
    tip: 'The shrine is directly across from Imam Hussain (AS) Haram. The path between them is called the road of Furat.'
  },
]

const walkInfo = [
  { icon: '📏', title: 'Distance', value: '~80 km from Najaf to Karbala' },
  { icon: '⏱', title: 'Duration', value: '3 days walking (average pace)' },
  { icon: '🧭', title: 'Poles', value: 'Poles numbered along the route for location tracking' },
  { icon: '🍽', title: 'Food & Water', value: 'Completely FREE — hundreds of Mawkabs serve all pilgrims' },
  { icon: '🌙', title: 'Night Walking', value: 'Many pilgrims prefer to walk at night when cooler' },
  { icon: '👟', title: 'Footwear', value: 'Comfortable, broken-in shoes or sandals. Many walk barefoot in the final stretch' },
  { icon: '🩺', title: 'Medical', value: 'First aid stations every few km along the route' },
  { icon: '📱', title: 'Group Contact', value: 'Keep group leader contact saved. Share your pole number when stopping' },
]
</script>

<template>
  <div class="min-h-screen pb-10">
    <!-- Header -->
    <div class="relative overflow-hidden border-b border-gray-200 bg-gradient-to-br from-amber-50 to-white">
      <div class="relative px-4 md:px-6 py-8">
        <div class="text-center">
          <p class="text-amber-700 font-arabic text-2xl mb-2">یَا حُسَیْن</p>
          <h1 class="text-3xl md:text-4xl font-bold text-gray-900">Misbah ul Hoda</h1>
          <p class="text-amber-600 text-xs mt-0.5">misbahulhoda.org</p>
          <p class="text-amber-700 text-lg mt-2 font-semibold">Arbaeen 2026 — Complete Tour Guide</p>
          <p class="text-gray-700 text-sm mt-2">14 Days · 3 Holy Cities · The World's Greatest Pilgrimage</p>
          <div class="flex flex-wrap justify-center gap-3 mt-4">
            <span class="bg-amber-100 text-amber-700 text-xs px-3 py-1.5 rounded-full border border-amber-300">2 Aug – 15 Aug 2026</span>
            <span class="bg-green-100 text-green-700 text-xs px-3 py-1.5 rounded-full border border-green-300">Kadhimiyyah · Najaf · Karbala</span>
            <span class="bg-gray-100 text-gray-600 text-xs px-3 py-1.5 rounded-full border border-gray-300">Arbaeen Walk ~80km</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Tabs -->
    <div class="sticky top-0 z-10 bg-white border-b border-gray-200 overflow-x-auto">
      <div class="flex min-w-max px-4">
        <button
          v-for="tab in tabs" :key="tab.id"
          @click="activeTab = tab.id"
          :class="['flex items-center gap-1.5 px-4 py-3.5 text-sm font-medium border-b-2 transition-colors whitespace-nowrap',
            activeTab === tab.id
              ? 'border-amber-600 text-amber-700'
              : 'border-transparent text-gray-700 hover:text-gray-900']">
          <span>{{ tab.icon }}</span>{{ tab.label }}
        </button>
      </div>
    </div>

    <div class="px-4 md:px-6 mt-6">

      <!-- SCHEDULE TAB -->
      <div v-if="activeTab === 'schedule'" class="space-y-3 max-w-3xl mx-auto">
        <div class="text-center mb-6">
          <h2 class="text-xl font-bold text-gray-900">14-Day Itinerary</h2>
          <p class="text-gray-700 text-sm mt-1">Tap on any day to see the full schedule</p>
        </div>

        <div v-for="day in itinerary" :key="day.day" class="card overflow-hidden">
          <button
            class="w-full text-left"
            @click="expandedDay = expandedDay === day.day ? null : day.day">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-4">
                <div class="w-11 h-11 rounded-xl bg-amber-100 border border-amber-300 flex flex-col items-center justify-center shrink-0">
                  <span class="text-xs text-amber-600 leading-none">Day</span>
                  <span class="text-lg font-bold text-amber-700 leading-none">{{ day.day }}</span>
                </div>
                <div>
                  <p class="font-semibold text-gray-900 text-sm">{{ day.theme }}</p>
                  <p class="text-gray-600 text-xs mt-0.5">{{ day.date }}</p>
                  <p class="text-amber-700 text-xs">📍 {{ day.city }}</p>
                </div>
              </div>
              <span class="text-gray-600 text-lg transition-transform" :class="expandedDay === day.day ? 'rotate-180' : ''">▾</span>
            </div>
          </button>

          <div v-if="expandedDay === day.day" class="mt-4 pt-4 border-t border-gray-200 space-y-2">
            <div v-for="(event, i) in day.events" :key="i" class="flex gap-3">
              <div class="w-20 shrink-0">
                <span class="text-xs text-amber-700 font-medium">{{ event.time }}</span>
              </div>
              <div class="flex-1">
                <p class="text-sm text-gray-700">{{ event.activity }}</p>
              </div>
            </div>
            <div class="mt-3 bg-amber-50 border border-amber-200 rounded-lg p-3">
              <p class="text-xs text-amber-700">💡 {{ day.notes }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- HOTELS TAB -->
      <div v-if="activeTab === 'hotels'" class="space-y-6 max-w-2xl mx-auto">
        <div class="text-center mb-2">
          <h2 class="text-xl font-bold text-gray-900">Our Hotels</h2>
          <p class="text-gray-700 text-sm mt-1">Accommodation across the 3 holy cities</p>
        </div>

        <div v-if="hotelsLoading" class="text-center py-10 text-gray-700 text-sm">Loading hotels...</div>
        <div v-else-if="!hotels.length" class="text-center py-10 text-gray-600 text-sm">No hotels found.</div>

        <div v-for="hotel in hotels" :key="hotel.id || hotel.name" :class="['card border-l-4', hotel.color || 'border-gray-300']">
          <!-- Header -->
          <div class="flex items-start justify-between mb-3">
            <div>
              <div class="flex items-center gap-2 mb-1">
                <span class="text-2xl">{{ hotel.icon }}</span>
                <span class="text-xs font-bold uppercase tracking-wider text-amber-700">{{ hotel.city }}</span>
              </div>
              <h3 class="text-xl font-bold text-gray-900">{{ hotel.name }}</h3>
              <p class="text-gray-700 text-sm mt-1">📍 {{ hotel.address }}</p>
            </div>
            <span v-if="hotel.nights" class="bg-gray-100 text-gray-600 text-xs px-2 py-1 rounded-full shrink-0 ml-2">{{ hotel.nights }}</span>
          </div>

          <!-- Distance row -->
          <div v-if="hotel.nearHaram" class="flex items-center gap-2 mb-3 bg-green-50 border border-green-200 rounded-lg px-3 py-2">
            <span class="text-green-600 text-sm">🚶</span>
            <div class="flex-1">
              <span class="text-green-700 text-sm font-medium">{{ hotel.nearHaram }}</span>
              <span v-if="hotel.haramDistance" class="text-gray-700 text-xs ml-2">({{ hotel.haramDistance }})</span>
            </div>
          </div>
          <!-- Maps + Phone buttons -->
          <div class="flex flex-wrap gap-2 mb-4">
            <a v-if="hotel.lat && hotel.lng"
               :href="`https://www.google.com/maps?q=${hotel.lat},${hotel.lng}`"
               target="_blank" rel="noopener noreferrer"
               class="flex items-center gap-1.5 bg-gray-100 hover:bg-gray-200 border border-gray-300 text-gray-700 text-xs px-3 py-1.5 rounded-lg transition-colors">
              📍 Hotel on Maps
            </a>
            <a v-if="hotel.lat && hotel.lng && hotel.haramLat && hotel.haramLng"
               :href="`https://www.google.com/maps/dir/${hotel.lat},${hotel.lng}/${hotel.haramLat},${hotel.haramLng}`"
               target="_blank" rel="noopener noreferrer"
               class="flex items-center gap-1.5 bg-green-100 hover:bg-green-200 border border-green-300 text-green-700 text-xs px-3 py-1.5 rounded-lg transition-colors">
              🕌 Directions to Haram
            </a>
            <a v-if="hotel.phone" :href="`tel:${hotel.phone}`"
               class="flex items-center gap-1.5 bg-gray-100 hover:bg-gray-200 border border-gray-300 text-gray-600 text-xs px-3 py-1.5 rounded-lg transition-colors">
              📞 {{ hotel.phone }}
            </a>
          </div>

          <!-- Jamat Timings -->
          <div v-if="hotel.jamat" class="mb-4">
            <p class="text-xs text-gray-700 uppercase font-medium mb-2 flex items-center gap-1.5">
              🕐 Namaz / Jamat Timings <span class="text-gray-600 normal-case">(approx. Aug 2026)</span>
            </p>
            <div class="grid grid-cols-5 gap-1">
              <div v-for="s in hotel.jamat" :key="s.salah"
                   class="bg-gray-50 rounded-lg px-2 py-2 text-center border border-gray-200">
                <p class="text-amber-700 text-xs font-bold mb-1">{{ s.salah }}</p>
                <p class="text-gray-700 text-xs leading-tight">{{ s.jamat }}</p>
                <p class="text-gray-600 text-xs leading-tight mt-0.5">Adhan {{ s.adhan }}</p>
              </div>
            </div>
            <p class="text-gray-600 text-xs mt-1.5">Times shown are Jamat (congregational prayer) at the Haram. Arrive 10 min early.</p>
          </div>

          <!-- Amenities -->
          <div class="mb-4">
            <p class="text-xs text-gray-700 uppercase font-medium mb-2">Amenities</p>
            <div class="flex flex-wrap gap-2">
              <span v-for="a in hotel.amenities" :key="a" class="bg-gray-100 text-gray-600 text-xs px-2.5 py-1 rounded-full">{{ a }}</span>
            </div>
          </div>

          <div class="bg-amber-50 border border-amber-200 rounded-lg p-3">
            <p class="text-xs text-amber-700">💡 Tip: {{ hotel.tips }}</p>
          </div>
        </div>

        <!-- Walk accommodation note -->
        <div class="card border border-dashed border-gray-300">
          <div class="flex gap-3">
            <span class="text-3xl">🏕</span>
            <div>
              <h3 class="font-semibold text-gray-900 mb-1">Arbaeen Walk Accommodation</h3>
              <p class="text-gray-700 text-sm">During the 3-day walk (Nights 7–9), you'll stay at Mawkabs — free hospitality tents set up by Iraqi families and organizations along the route. All food, water, and basic bedding is provided completely free by the Iraqi people as an act of devotion.</p>
            </div>
          </div>
        </div>
      </div>

      <!-- SCHOLARS TAB -->
      <div v-if="activeTab === 'scholars'" class="max-w-3xl mx-auto">
        <div class="text-center mb-6">
          <h2 class="text-xl font-bold text-gray-900">Scholars & Reciters</h2>
          <p class="text-gray-700 text-sm mt-1">World-class Islamic scholars accompanying the Arbaeen 2026 tour</p>
          <div class="flex justify-center gap-3 mt-3 flex-wrap">
            <span class="text-xs bg-blue-100 text-blue-700 border border-blue-300 px-3 py-1 rounded-full">🇬🇧 UK · 🇺🇸 USA · 🇨🇦 Canada</span>
            <span class="text-xs bg-green-100 text-green-700 border border-green-300 px-3 py-1 rounded-full">🇵🇰 Pakistan</span>
            <span class="text-xs bg-gray-100 text-gray-600 border border-gray-300 px-3 py-1 rounded-full">English & Urdu</span>
          </div>
        </div>

        <!-- Loading -->
        <div v-if="scholarsLoading" class="text-center py-8 text-gray-700 text-sm">Loading scholars...</div>

        <!-- Scholars -->
        <div v-else class="mb-8">
          <h3 class="text-sm font-bold uppercase tracking-wider text-amber-700 mb-4 flex items-center gap-2">
            <span>📚</span> Islamic Scholars
          </h3>
          <div class="space-y-5">
            <div v-for="(s, i) in (useApi ? dynamicScholars : scholars)" :key="s.id || s.name" class="card">
              <!-- Header -->
              <div class="flex items-start gap-4 mb-4">
                <img v-if="s.photoUrl" :src="s.photoUrl" :alt="s.name"
                  class="w-16 h-16 rounded-2xl object-cover border-2 border-primary-800 flex-shrink-0 shadow-lg"
                  @error="e => e.target.style.display='none'" />
                <div v-else :class="['w-16 h-16 rounded-2xl bg-gradient-to-br flex items-center justify-center text-xl font-bold text-gray-900 flex-shrink-0 shadow-lg', s.avatarColor]">
                  {{ s.initials }}
                </div>
                <div class="flex-1 min-w-0">
                  <div class="flex items-start justify-between gap-2">
                    <div>
                      <p class="font-bold text-gray-900 text-base">{{ s.name }}</p>
                      <p class="text-amber-700 text-xs mt-0.5">{{ s.specialty }}</p>
                      <p class="text-gray-600 text-xs mt-0.5">{{ s.flag }} {{ s.location }}</p>
                    </div>
                    <span :class="['text-xs px-2 py-1 rounded-full font-medium flex-shrink-0', s.lang === 'English' ? 'bg-blue-100 text-blue-700 border border-blue-300' : 'bg-green-100 text-green-700 border border-green-300']">
                      {{ s.lang }}
                    </span>
                  </div>
                </div>
              </div>

              <!-- Quote -->
              <div v-if="s.quote" class="bg-amber-50 border-l-4 border-amber-500 rounded-r-lg px-4 py-3 mb-4">
                <p class="text-gray-700 text-sm italic leading-relaxed">{{ s.quote }}</p>
              </div>

              <!-- Bio -->
              <p class="text-gray-700 text-sm leading-relaxed mb-4">{{ s.bio }}</p>

              <!-- Tags -->
              <div class="flex flex-wrap gap-2">
                <span v-for="tag in s.tags" :key="tag"
                  class="text-xs bg-gray-100 text-gray-700 border border-gray-300 px-2.5 py-1 rounded-full">
                  {{ tag }}
                </span>
                <span v-if="s.youtube && !s.youtubeUrl" class="text-xs bg-amber-100 text-amber-700 border border-amber-300 px-2.5 py-1 rounded-full">
                  🎥 {{ s.youtube }}
                </span>
              </div>
              <!-- YouTube + Website links -->
              <div v-if="s.youtubeUrl || s.website" class="flex flex-wrap gap-2 mt-2">
                <a v-if="s.youtubeUrl" :href="s.youtubeUrl" target="_blank" rel="noopener"
                  class="flex items-center gap-1.5 text-xs bg-red-100 text-red-600 border border-red-300 px-3 py-1.5 rounded-full hover:bg-red-200 transition-colors font-medium">
                  ▶ YouTube Channel
                </a>
                <a v-if="s.website" :href="s.website" target="_blank" rel="noopener"
                  class="flex items-center gap-1.5 text-xs bg-gray-100 text-gray-600 border border-gray-300 px-3 py-1.5 rounded-full hover:bg-gray-200 transition-colors">
                  🌐 Website
                </a>
              </div>
            </div>
          </div>
        </div>

        <!-- Reciters -->
        <div v-if="(useApi ? dynamicReciters : reciters).length">
          <h3 class="text-sm font-bold uppercase tracking-wider text-amber-700 mb-4 flex items-center gap-2">
            <span>🎤</span> Noha Khuwan & Reciters
          </h3>
          <div class="space-y-4">
            <div v-for="r in (useApi ? dynamicReciters : reciters)" :key="r.id || r.name" class="card">
              <div class="flex items-start gap-4 mb-3">
                <img v-if="r.photoUrl" :src="r.photoUrl" :alt="r.name"
                  class="w-14 h-14 rounded-2xl object-cover border-2 border-gold-800 flex-shrink-0"
                  @error="e => e.target.style.display='none'" />
                <div v-else :class="['w-14 h-14 rounded-2xl bg-gradient-to-br flex items-center justify-center text-lg font-bold text-gray-900 flex-shrink-0', r.avatarColor]">
                  {{ r.initials }}
                </div>
                <div class="flex-1">
                  <div class="flex items-start justify-between">
                    <div>
                      <p class="font-bold text-gray-900">{{ r.name }}</p>
                      <p class="text-amber-700 text-xs mt-0.5">{{ r.specialty }}</p>
                      <p class="text-gray-600 text-xs">{{ r.flag }} {{ r.location }}</p>
                    </div>
                    <span :class="['text-xs px-2 py-0.5 rounded-full', r.lang === 'English' ? 'bg-blue-100 text-blue-700' : 'bg-green-100 text-green-700']">
                      {{ r.lang }}
                    </span>
                  </div>
                </div>
              </div>
              <p v-if="r.bio" class="text-gray-700 text-sm leading-relaxed mb-3">{{ r.bio }}</p>
              <p v-else-if="r.note" class="text-gray-700 text-sm leading-relaxed mb-3">{{ r.note }}</p>
              <div class="flex flex-wrap gap-2">
                <span v-for="tag in r.tags" :key="tag" class="text-xs bg-gray-100 text-gray-700 border border-gray-200 px-2 py-0.5 rounded-full">{{ tag }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ARBAEEN WALK TAB -->
      <div v-if="activeTab === 'walk'" class="max-w-2xl mx-auto">
        <div class="text-center mb-6">
          <div class="text-5xl mb-3">🚶</div>
          <h2 class="text-xl font-bold text-gray-900">The Arbaeen Walk</h2>
          <p class="text-gray-700 text-sm mt-1">The world's largest human gathering — a walk of love and devotion</p>
        </div>

        <!-- Highlight Banner -->
        <div class="bg-gradient-to-r from-amber-50 to-green-50 border border-amber-200 rounded-xl p-5 mb-6 text-center">
          <p class="text-amber-700 text-sm font-medium">Every year, over</p>
          <p class="text-4xl font-bold text-gray-900 my-1">20 Million+</p>
          <p class="text-amber-700 text-sm">pilgrims walk from Najaf to Karbala in one of the greatest acts of love in human history.</p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
          <div v-for="info in walkInfo" :key="info.title" class="card flex gap-3">
            <span class="text-2xl">{{ info.icon }}</span>
            <div>
              <p class="text-xs text-gray-600 uppercase font-medium">{{ info.title }}</p>
              <p class="text-gray-900 text-sm font-medium mt-0.5">{{ info.value }}</p>
            </div>
          </div>
        </div>

        <div class="card mb-4">
          <h3 class="font-semibold text-gray-900 mb-3">What are Mawkabs? 🏕</h3>
          <p class="text-gray-700 text-sm leading-relaxed">Mawkabs are free hospitality tents set up by Iraqi families, organizations, and communities along the entire 80km route. They provide free food, water, tea, medical care, and sleeping space to all pilgrims — regardless of nationality or religion. This is considered one of the most remarkable acts of generosity in human history, repeated every year.</p>
        </div>

        <div class="card">
          <h3 class="font-semibold text-gray-900 mb-3">Essential Walk Tips</h3>
          <ul class="space-y-2.5 text-sm">
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700 mt-0.5">✓</span>Break in your shoes/sandals well before the trip</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700 mt-0.5">✓</span>Carry only essential items — light backpack is best</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700 mt-0.5">✓</span>Stay with your group and know the pole numbers</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700 mt-0.5">✓</span>Drink water constantly even if not thirsty</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700 mt-0.5">✓</span>Many pilgrims walk barefoot the last 3km into Karbala</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700 mt-0.5">✓</span>Recite Ziyarat Arbaeen as you walk — this is the Sunnah of the Imams</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700 mt-0.5">✓</span>If you cannot walk, vehicles are available at additional cost</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-600 mt-0.5">⚠</span>August is hot. Start walking early morning or after sunset</li>
          </ul>
        </div>
      </div>

      <!-- HOLY SITES TAB -->
      <div v-if="activeTab === 'maps'" class="space-y-4 max-w-2xl mx-auto">
        <div class="text-center mb-6">
          <h2 class="text-xl font-bold text-gray-900">Holy Sites & Ziyarat</h2>
          <p class="text-gray-700 text-sm mt-1">Guide to the sacred shrines and mosques on the tour</p>
        </div>

        <div v-for="site in holySites" :key="site.name" class="card">
          <div class="flex items-start gap-4 mb-3">
            <span class="text-3xl">{{ site.icon }}</span>
            <div class="flex-1">
              <h3 class="font-bold text-gray-900">{{ site.name }}</h3>
              <p class="text-amber-700 text-xs mt-0.5">📍 {{ site.city }}</p>
            </div>
          </div>
          <p class="text-gray-600 text-sm mb-3">{{ site.description }}</p>
          <div class="bg-amber-50 border border-amber-200 rounded-lg p-3 mb-2">
            <p class="text-xs text-amber-700 font-semibold uppercase mb-1">Ziyarat Guide</p>
            <p class="text-gray-600 text-sm">{{ site.ziyarat }}</p>
          </div>
          <div class="bg-green-50 border border-green-200 rounded-lg p-3">
            <p class="text-xs text-green-700">💡 {{ site.tip }}</p>
          </div>
        </div>
      </div>

      <!-- TEAM TAB -->
      <div v-if="activeTab === 'team'" class="max-w-2xl mx-auto">
        <div class="text-center mb-6">
          <h2 class="text-xl font-bold text-gray-900">Our Team — Khuddam Al-Hussain</h2>
          <p class="text-gray-700 text-sm mt-1">Dedicated servants of Imam Hussain (A.S.) accompanying the pilgrimage</p>
        </div>

        <!-- WhatsApp Group CTA -->
        <div class="mb-5 rounded-2xl overflow-hidden border border-green-300 bg-green-50">
          <div class="p-5 flex flex-col sm:flex-row items-center gap-4">
            <div class="w-14 h-14 rounded-full bg-green-100 border border-green-300 flex items-center justify-center text-3xl flex-shrink-0">
              💬
            </div>
            <div class="flex-1 text-center sm:text-left">
              <h3 class="text-gray-900 font-bold text-base">Join the Pilgrim WhatsApp Group</h3>
              <p class="text-green-700 text-sm mt-0.5">Stay updated on schedule, announcements and get direct access to the team</p>
            </div>
            <a href="https://chat.whatsapp.com/misbahulhoda2026" target="_blank"
               class="flex-shrink-0 px-5 py-2.5 rounded-xl font-semibold text-sm text-white transition-all"
               style="background: linear-gradient(135deg, #16a34a, #15803d);">
              Join Group →
            </a>
          </div>
        </div>

        <!-- Direct Contacts -->
        <div class="card mb-5">
          <h3 class="font-semibold text-gray-900 mb-3 flex items-center gap-2">
            <span>📞</span> Direct Contact — Misbah ul Hoda
          </h3>
          <div class="space-y-2">
            <a href="https://wa.me/447825880549" target="_blank"
               class="flex items-center gap-3 bg-gray-50 hover:bg-gray-100 border border-gray-200 rounded-lg px-4 py-3 transition-colors group">
              <span class="text-green-600 text-xl">📱</span>
              <div class="flex-1">
                <p class="text-xs text-gray-600 uppercase font-medium">UK — WhatsApp</p>
                <p class="text-gray-900 font-medium text-sm">+44 782 588 0549</p>
              </div>
              <span class="text-green-600 text-xs opacity-0 group-hover:opacity-100 transition-opacity">Open →</span>
            </a>
            <a href="https://wa.me/447305307650" target="_blank"
               class="flex items-center gap-3 bg-gray-50 hover:bg-gray-100 border border-gray-200 rounded-lg px-4 py-3 transition-colors group">
              <span class="text-green-600 text-xl">📱</span>
              <div class="flex-1">
                <p class="text-xs text-gray-600 uppercase font-medium">UK — WhatsApp</p>
                <p class="text-gray-900 font-medium text-sm">+44 730 530 7650</p>
              </div>
              <span class="text-green-600 text-xs opacity-0 group-hover:opacity-100 transition-opacity">Open →</span>
            </a>
            <a href="https://wa.me/14254435786" target="_blank"
               class="flex items-center gap-3 bg-gray-50 hover:bg-gray-100 border border-gray-200 rounded-lg px-4 py-3 transition-colors group">
              <span class="text-green-600 text-xl">📱</span>
              <div class="flex-1">
                <p class="text-xs text-gray-600 uppercase font-medium">USA — WhatsApp</p>
                <p class="text-gray-900 font-medium text-sm">+1 425 443 5786</p>
              </div>
              <span class="text-green-600 text-xs opacity-0 group-hover:opacity-100 transition-opacity">Open →</span>
            </a>
            <a href="mailto:info@misbahulhoda.org"
               class="flex items-center gap-3 bg-gray-50 hover:bg-gray-100 border border-gray-200 rounded-lg px-4 py-3 transition-colors group">
              <span class="text-amber-700 text-xl">✉️</span>
              <div class="flex-1">
                <p class="text-xs text-gray-600 uppercase font-medium">Email</p>
                <p class="text-gray-900 font-medium text-sm">info@misbahulhoda.org</p>
              </div>
              <span class="text-amber-700 text-xs opacity-0 group-hover:opacity-100 transition-opacity">Open →</span>
            </a>
          </div>
        </div>

        <!-- Team List -->
        <div class="card mb-5">
          <div class="flex items-center gap-3 mb-4">
            <span class="text-2xl">🤲</span>
            <div>
              <h3 class="font-semibold text-gray-900">Khuddam Al-Hussain A.S.</h3>
              <p class="text-gray-600 text-xs">On-call 24/7 throughout the pilgrimage</p>
            </div>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-2">
            <div v-for="(member, i) in khuddamTeam" :key="i"
                 class="flex items-center gap-3 bg-gray-50 border border-gray-200 rounded-lg px-3 py-2.5">
              <div class="w-8 h-8 rounded-full bg-amber-100 border border-amber-300 flex items-center justify-center text-xs font-bold text-amber-700 flex-shrink-0">
                {{ member.name.split(' ').map(n => n[0]).slice(0,2).join('') }}
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-gray-900 text-sm font-medium">{{ member.name }}</p>
                <p class="text-gray-600 text-xs">{{ member.location }}</p>
              </div>
            </div>
          </div>
        </div>

        <div class="card">
          <h3 class="font-semibold text-gray-900 mb-3">Emergency Contacts During Tour</h3>
          <div class="space-y-3">
            <div class="bg-gray-50 border border-gray-200 rounded-lg p-3">
              <p class="text-xs text-gray-600 uppercase font-medium">Group Leader</p>
              <p class="text-gray-900 font-medium mt-1">Contact via Pilgrim WhatsApp Group (pinned message)</p>
            </div>
            <div class="bg-gray-50 border border-gray-200 rounded-lg p-3">
              <p class="text-xs text-gray-600 uppercase font-medium">Emergency Helpline (Iraq)</p>
              <p class="text-gray-900 font-medium mt-1">+964 7XX XXX XXXX — Available 24/7</p>
            </div>
            <div class="bg-gray-50 border border-gray-200 rounded-lg p-3">
              <p class="text-xs text-gray-600 uppercase font-medium">Pakistan Embassy Baghdad</p>
              <p class="text-gray-900 font-medium mt-1">+964 7XX XXX XXXX</p>
            </div>
          </div>
        </div>

        <!-- Important Reminders -->
        <div class="mt-6 card border border-amber-300 bg-amber-50">
          <h3 class="font-semibold text-amber-700 mb-3 flex items-center gap-2">⭐ Important Reminders</h3>
          <ul class="space-y-2 text-sm">
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700">•</span>Always carry your passport and visa documents</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700">•</span>Keep emergency cash in USD and Iraqi Dinars</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700">•</span>Download offline maps before departure</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700">•</span>Inform family of your itinerary and check-in points</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700">•</span>Dress modestly at all times — this is sacred ground</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700">•</span>Follow group leader instructions, especially in crowds</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700">•</span>Take prescribed medications and carry a basic first aid kit</li>
            <li class="flex gap-2 text-gray-700"><span class="text-amber-700">•</span>Join the WhatsApp group immediately — all updates shared there</li>
          </ul>
        </div>
      </div>

    </div>
  </div>
</template>
