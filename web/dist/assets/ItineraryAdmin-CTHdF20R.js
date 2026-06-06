import{A as e,C as t,E as n,I as ee,N as r,O as i,P as a,R as o,S as s,T as c,_ as te,b as ne,f as l,g as u,k as d,p as f,w as p,x as m,y as h}from"./index-Ba7OHL5y.js";import{o as g}from"./api-LnTyzOfs.js";var re={class:`p-6 space-y-6`},ie={key:0,class:`bg-green-900/50 border border-green-700 text-green-300 rounded-lg px-4 py-3 flex justify-between`},ae={key:1,class:`bg-red-900/50 border border-red-700 text-red-300 rounded-lg px-4 py-3 flex justify-between`},oe={class:`flex items-center justify-between flex-wrap gap-3`},se={class:`flex gap-1 bg-dark-800 rounded-xl p-1 border border-dark-600`},ce=[`onClick`],le=[`disabled`],ue={key:0,class:`animate-spin`},de={key:1},fe={key:2,class:`text-center py-20 text-slate-400`},pe={class:`grid grid-cols-2 md:grid-cols-4 gap-4`},me={class:`card text-center py-4`},he={class:`text-3xl font-bold text-green-400`},ge={class:`card text-center py-4`},_e={class:`text-3xl font-bold text-blue-400`},ve={class:`card text-center py-4`},ye={class:`text-3xl font-bold text-red-400`},be={class:`card text-center py-4`},xe={class:`text-3xl font-bold text-orange-400`},Se={class:`card`},Ce={class:`text-white font-semibold mb-4`},we={class:`text-slate-500 font-normal text-sm`},Te={key:0},Ee={key:1,class:`text-center py-8 text-slate-500 text-sm`},De={class:`card`},Oe={class:`text-white font-semibold mb-4`},ke={class:`text-slate-500 font-normal text-sm`},Ae={key:0},je={key:1,class:`text-center py-8 text-slate-500 text-sm`},Me={class:`flex items-center gap-4 flex-wrap`},Ne={class:`flex items-center gap-2`},Pe={key:0,class:`card text-center py-16 text-slate-500`},_={class:`flex items-center justify-between`},v={class:`text-white font-bold text-base`},y={class:`flex gap-3 text-xs`},Fe={key:0,class:`text-green-400 bg-green-900/40 px-2 py-0.5 rounded-full`},Ie={key:1,class:`text-blue-400 bg-blue-900/40 px-2 py-0.5 rounded-full`},Le={key:0},Re={key:1},ze={class:`flex items-center gap-3`},Be={class:`card`},Ve={class:`text-white font-semibold mb-4`},He={class:`text-slate-500 font-normal text-sm`},Ue={key:0},We={key:1,class:`text-center py-12 text-slate-500`},Ge={class:`flex items-center gap-3`},Ke={class:`card`},qe={class:`text-white font-semibold mb-4`},Je={class:`text-slate-500 font-normal text-sm`},Ye={key:0},Xe={key:1,class:`text-center py-12 text-slate-500`},Ze={key:0,class:`fixed inset-0 bg-black/70 z-50 flex items-center justify-center p-4`},Qe={class:`bg-dark-900 border border-dark-600 rounded-2xl p-6 w-full max-w-lg shadow-2xl max-h-[90vh] overflow-y-auto`},$e={class:`flex items-center justify-between mb-5`},et={class:`text-white font-bold text-lg`},tt={class:`text-slate-400 text-sm mt-0.5`},nt={key:0},rt={class:`space-y-3`},it={class:`grid grid-cols-2 gap-3`},b={class:`grid grid-cols-2 gap-3`},at=[`value`],ot={class:`label`},st=[`placeholder`],ct={class:`flex gap-3 mt-5`},lt=[`disabled`],x=Object.assign({components:{UpcomingMiniTable:{props:{rows:Array,statusColors:Object,statusIcons:Object,wa:Function},emits:[`assign`],template:`
        <div class="overflow-x-auto">
          <table class="w-full text-xs">
            <thead>
              <tr class="text-slate-600 border-b border-dark-700 uppercase">
                <th class="text-left pb-1.5 font-medium pr-3">Pilgrim</th>
                <th class="text-left pb-1.5 font-medium pr-3">Flight / Airport</th>
                <th class="text-left pb-1.5 font-medium pr-3">Time</th>
                <th class="text-left pb-1.5 font-medium pr-3">Family</th>
                <th class="text-left pb-1.5 font-medium pr-3">Pickup Status</th>
                <th class="pb-1.5"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in rows" :key="row.pilgrimId" class="border-b border-dark-800 hover:bg-dark-800/30">
                <td class="py-2 pr-3">
                  <p class="text-white font-semibold">{{ row.fullName }}</p>
                  <p class="text-slate-500">{{ row.country }}</p>
                  <a v-if="wa(row.whatsApp || row.phone)" :href="wa(row.whatsApp || row.phone)"
                    target="_blank" class="text-green-400 hover:underline">💬 WA</a>
                </td>
                <td class="py-2 pr-3">
                  <p class="text-white">{{ row.flightNumber || '—' }}</p>
                  <p class="text-slate-500">{{ row.airport || 'Airport not set' }}</p>
                </td>
                <td class="py-2 pr-3 text-white">{{ row.time || '—' }}</td>
                <td class="py-2 pr-3">
                  <span class="text-gold-400 font-semibold">{{ row.familyCount }}</span>
                </td>
                <td class="py-2 pr-3">
                  <span :class="['px-2 py-0.5 rounded-full font-medium', statusColors[row.transferStatus] || 'text-slate-400 bg-slate-800']">
                    {{ statusIcons[row.transferStatus] }} {{ row.transferStatus }}
                  </span>
                  <p v-if="row.transfer?.driverName" class="text-slate-400 mt-0.5">
                    🚗 {{ row.transfer.driverName }}
                  </p>
                </td>
                <td class="py-2">
                  <button @click="$emit('assign', row)"
                    class="px-2 py-1 rounded bg-primary-800 hover:bg-primary-700 text-white whitespace-nowrap">
                    {{ row.transfer ? '✏️ Edit' : '+ Assign' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      `},PilgrimTransferRows:{props:{rows:Array,type:Number,statusColors:Object,statusIcons:Object,statusOptions:Array,updatingId:String,wa:Function,fmtDate:Function,fmtTime:Function},emits:[`assign`,`status`,`remove`],template:`
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="text-slate-500 border-b border-dark-600 text-xs uppercase">
                <th class="text-left pb-2 font-medium pr-3">Pilgrim</th>
                <th class="text-left pb-2 font-medium pr-3">Flight / Airport</th>
                <th class="text-left pb-2 font-medium pr-3">Date & Time</th>
                <th class="text-left pb-2 font-medium pr-3">Family</th>
                <th class="text-left pb-2 font-medium pr-3">Transfer Status</th>
                <th class="text-left pb-2 font-medium pr-3">Driver / Vehicle</th>
                <th class="pb-2"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in rows" :key="row.pilgrimId" class="border-b border-dark-700 hover:bg-dark-800/40">
                <td class="py-3 pr-3">
                  <p class="text-white font-semibold">{{ row.fullName }}</p>
                  <p class="text-slate-500 text-xs">{{ row.country }}</p>
                  <a v-if="wa(row.whatsApp || row.phone)" :href="wa(row.whatsApp || row.phone)"
                    target="_blank" class="text-xs text-green-400 hover:underline">💬 WhatsApp</a>
                </td>
                <td class="py-3 pr-3">
                  <p class="text-white">{{ row.flightNumber || '—' }}</p>
                  <p class="text-slate-400 text-xs">{{ row.airport || 'Airport not set' }}</p>
                </td>
                <td class="py-3 pr-3">
                  <p class="text-white">{{ fmtDate(row.date) }}</p>
                  <p class="text-slate-400 text-xs">{{ row.time || 'Time not set' }}</p>
                </td>
                <td class="py-3 pr-3">
                  <span class="text-gold-400 font-semibold">{{ row.familyCount }}</span>
                  <span class="text-slate-500 text-xs"> member{{ row.familyCount !== 1 ? 's' : '' }}</span>
                </td>
                <td class="py-3 pr-3">
                  <div class="flex items-center gap-2">
                    <span :class="['text-xs px-2 py-0.5 rounded-full font-medium', statusColors[row.transferStatus] || 'text-slate-400 bg-slate-800']">
                      {{ statusIcons[row.transferStatus] }} {{ row.transferStatus }}
                    </span>
                    <select v-if="row.transferId"
                      :disabled="updatingId === row.transferId"
                      @change="$emit('status', row.transferId, $event.target.value)"
                      class="text-xs bg-dark-700 border border-dark-600 text-slate-300 rounded px-1 py-0.5">
                      <option v-for="s in statusOptions" :key="s.value" :value="s.value">{{ s.label }}</option>
                    </select>
                  </div>
                  <div v-if="row.transfer?.scheduledTime" class="text-xs text-slate-500 mt-1">
                    🕐 {{ fmtTime(row.transfer.scheduledTime) }}
                  </div>
                </td>
                <td class="py-3 pr-3">
                  <div v-if="row.transfer?.driverName">
                    <p class="text-white text-xs font-semibold">{{ row.transfer.driverName }}</p>
                    <p class="text-slate-400 text-xs">{{ row.transfer.vehicleType }} · {{ row.transfer.vehicleNumber || '—' }}</p>
                    <a v-if="wa(row.transfer.driverPhone)" :href="wa(row.transfer.driverPhone)"
                      target="_blank" class="text-xs text-green-400 hover:underline">📞 {{ row.transfer.driverPhone }}</a>
                    <p v-if="row.transfer.meetingPoint" class="text-slate-500 text-xs mt-0.5">📍 {{ row.transfer.meetingPoint }}</p>
                  </div>
                  <span v-else class="text-slate-600 text-xs italic">Not assigned</span>
                </td>
                <td class="py-3">
                  <div class="flex flex-col gap-1">
                    <button @click="$emit('assign', row, type)"
                      class="text-xs px-2 py-1 rounded bg-primary-800 hover:bg-primary-700 text-white whitespace-nowrap">
                      {{ row.transfer ? '✏️ Edit' : '+ Assign' }}
                    </button>
                    <button v-if="row.transferId" @click="$emit('remove', row.transferId)"
                      class="text-xs px-2 py-1 rounded bg-red-900/50 hover:bg-red-900 text-red-400 whitespace-nowrap">
                      Remove
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      `}}},{__name:`ItineraryAdmin`,setup(x){let S=a(`today`),C=a(!1),w=a(!1),T=a(!1),E=a(``),D=a(``),O=a(null),k=a([]),A=a([]),j=a(null),M=a(7),N=a(new Date().toISOString().split(`T`)[0]),P=a(new Date().toISOString().split(`T`)[0]),F=a(!1),I=a(null),L=a(1),R=a({driverName:``,driverPhone:``,vehicleNumber:``,vehicleType:`Car`,meetingPoint:``,scheduledTime:``,notes:``}),z=a(null),ut=[`Car`,`Van`,`Coaster`,`Bus`,`Ambulance`,`Other`],B={Unassigned:`text-slate-400 bg-slate-800`,Pending:`text-gold-400 bg-gold-900/50`,DriverAssigned:`text-blue-400 bg-blue-900/50`,EnRoute:`text-purple-400 bg-purple-900/50`,Completed:`text-green-400 bg-green-900/50`,Cancelled:`text-red-400 bg-red-900/40`},V={Unassigned:`⚪`,Pending:`⏳`,DriverAssigned:`🚗`,EnRoute:`🛣️`,Completed:`✅`,Cancelled:`❌`},H=[{value:1,label:`Pending`},{value:2,label:`Driver Assigned`},{value:3,label:`En Route`},{value:4,label:`Completed`},{value:5,label:`Cancelled`}];function U(e){return e?new Date(e).toLocaleDateString(`en-GB`,{day:`2-digit`,month:`short`,year:`numeric`}):`—`}function W(e){return e?new Date(e).toLocaleString(`en-GB`,{day:`2-digit`,month:`short`,hour:`2-digit`,minute:`2-digit`}):`—`}function G(e){return e?`https://wa.me/`+e.replace(/\D/g,``):null}async function K(){C.value=!0,E.value=``;try{O.value=(await g.getToday()).data.data}catch(e){E.value=e.response?.data?.message||`Failed to load.`}finally{C.value=!1}}async function q(){C.value=!0,E.value=``;try{k.value=(await g.getArrivals(N.value)).data.data}catch(e){E.value=e.response?.data?.message||`Failed to load.`}finally{C.value=!1}}async function J(){C.value=!0,E.value=``;try{A.value=(await g.getDepartures(P.value)).data.data}catch(e){E.value=e.response?.data?.message||`Failed to load.`}finally{C.value=!1}}async function Y(){C.value=!0,E.value=``;try{j.value=(await g.getUpcoming(M.value)).data.data}catch(e){E.value=e.response?.data?.message||`Failed to load.`}finally{C.value=!1}}async function dt(){T.value=!0;try{await g.sendReminders(),D.value=`Reminders are being sent via WhatsApp & Email to all pilgrims with upcoming travel.`}catch(e){E.value=e.response?.data?.message||`Failed.`}finally{T.value=!1}}async function ft(e){S.value=e,e===`today`&&await K(),e===`arrivals`&&await q(),e===`departures`&&await J(),e===`upcoming`&&await Y()}async function X(){S.value===`today`&&await K(),S.value===`arrivals`&&await q(),S.value===`departures`&&await J(),S.value===`upcoming`&&await Y()}function Z(e,t){I.value=e,L.value=t;let n=e.transfer;R.value={driverName:n?.driverName||``,driverPhone:n?.driverPhone||``,vehicleNumber:n?.vehicleNumber||``,vehicleType:n?.vehicleType||`Car`,meetingPoint:n?.meetingPoint||``,scheduledTime:n?.scheduledTime?n.scheduledTime.slice(0,16):``,notes:n?.notes||``},F.value=!0}async function pt(){w.value=!0,E.value=``;try{await g.assignDriver(I.value.pilgrimId,{transferType:L.value,driverName:R.value.driverName,driverPhone:R.value.driverPhone,vehicleNumber:R.value.vehicleNumber,vehicleType:R.value.vehicleType,meetingPoint:R.value.meetingPoint,scheduledTime:R.value.scheduledTime?new Date(R.value.scheduledTime).toISOString():null,notes:R.value.notes}),D.value=`Driver assigned!`,F.value=!1,await X()}catch(e){E.value=e.response?.data?.message||`Failed to assign.`}finally{w.value=!1}}async function Q(e,t){z.value=e;try{await g.updateStatus(e,parseInt(t)),D.value=`Status updated!`,await X()}catch(e){E.value=e.response?.data?.message||`Failed.`}finally{z.value=null}}async function $(e){if(confirm(`Remove this driver assignment?`))try{await g.deleteTransfer(e),D.value=`Assignment removed.`,await X()}catch{E.value=`Failed to remove.`}}return n(K),(n,a)=>{let g=e(`PilgrimTransferRows`),x=e(`UpcomingMiniTable`);return i(),s(`div`,re,[a[38]||=h(`div`,null,[h(`h1`,{class:`text-2xl font-bold text-white`},`✈️ Itinerary & Airport Transfers`),h(`p`,{class:`text-slate-400 text-sm mt-1`},`Manage pilgrim arrivals, departures and airport pickup/drop assignments`)],-1),D.value?(i(),s(`div`,ie,[p(o(D.value),1),h(`button`,{onClick:a[0]||=e=>D.value=``,class:`text-green-500`},`✕`)])):m(``,!0),E.value?(i(),s(`div`,ae,[p(o(E.value),1),h(`button`,{onClick:a[1]||=e=>E.value=``,class:`text-red-500`},`✕`)])):m(``,!0),h(`div`,oe,[h(`div`,se,[(i(),s(u,null,d([[`today`,`📅 Today`],[`upcoming`,`📆 Upcoming`],[`arrivals`,`🛬 Arrivals`],[`departures`,`🛫 Departures`]],([e,t])=>h(`button`,{key:e,onClick:t=>ft(e),class:ee([`px-4 py-2 rounded-lg text-sm font-medium transition-all`,S.value===e?`bg-primary-700 text-white`:`text-slate-400 hover:text-white`])},o(t),11,ce)),64))]),h(`button`,{onClick:dt,disabled:T.value,class:`flex items-center gap-2 bg-gold-800 hover:bg-gold-700 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-xl transition-colors`},[T.value?(i(),s(`span`,ue,`⏳`)):(i(),s(`span`,de,`📲`)),p(` `+o(T.value?`Sending...`:`Send Reminders Now`),1)],8,le)]),C.value?(i(),s(`div`,fe,`Loading...`)):S.value===`today`&&O.value?(i(),s(u,{key:3},[h(`div`,pe,[h(`div`,me,[h(`div`,he,o(O.value.arrivingCount),1),a[16]||=h(`div`,{class:`text-slate-400 text-xs mt-1`},`🛬 Arriving Today`,-1)]),h(`div`,ge,[h(`div`,_e,o(O.value.departingCount),1),a[17]||=h(`div`,{class:`text-slate-400 text-xs mt-1`},`🛫 Departing Today`,-1)]),h(`div`,ve,[h(`div`,ye,o(O.value.pendingPickups),1),a[18]||=h(`div`,{class:`text-slate-400 text-xs mt-1`},`⚠️ Pickups Unassigned`,-1)]),h(`div`,be,[h(`div`,xe,o(O.value.pendingDropoffs),1),a[19]||=h(`div`,{class:`text-slate-400 text-xs mt-1`},`⚠️ Dropoffs Unassigned`,-1)])]),h(`div`,Se,[h(`h3`,Ce,[a[20]||=p(`🛬 Today's Arrivals `,-1),h(`span`,we,`(`+o(O.value.arrivals?.length||0)+`)`,1)]),O.value.arrivals?.length?(i(),s(`div`,Te,[c(g,{rows:O.value.arrivals,type:1,statusColors:B,statusIcons:V,statusOptions:H,updatingId:z.value,wa:G,fmtDate:U,fmtTime:W,onAssign:Z,onStatus:Q,onRemove:$},null,8,[`rows`,`updatingId`])])):(i(),s(`div`,Ee,`No arrivals today.`))]),h(`div`,De,[h(`h3`,Oe,[a[21]||=p(`🛫 Today's Departures `,-1),h(`span`,ke,`(`+o(O.value.departures?.length||0)+`)`,1)]),O.value.departures?.length?(i(),s(`div`,Ae,[c(g,{rows:O.value.departures,type:2,statusColors:B,statusIcons:V,statusOptions:H,updatingId:z.value,wa:G,fmtDate:U,fmtTime:W,onAssign:Z,onStatus:Q,onRemove:$},null,8,[`rows`,`updatingId`])])):(i(),s(`div`,je,`No departures today.`))])],64)):S.value===`upcoming`?(i(),s(u,{key:4},[h(`div`,Me,[h(`div`,Ne,[a[23]||=h(`label`,{class:`text-slate-400 text-sm`},`Show next`,-1),r(h(`select`,{"onUpdate:modelValue":a[2]||=e=>M.value=e,onChange:Y,class:`input w-auto text-sm`},[...a[22]||=[h(`option`,{value:3},`3 days`,-1),h(`option`,{value:7},`7 days`,-1),h(`option`,{value:14},`14 days`,-1),h(`option`,{value:30},`30 days`,-1)]],544),[[l,M.value,void 0,{number:!0}]])]),a[24]||=t(`<div class="flex flex-col gap-1"><p class="text-slate-500 text-sm">📅 <strong class="text-slate-400">8:00 AM UTC</strong> — 2-day advance reminders sent to all travellers.</p><p class="text-slate-500 text-sm">⏱️ <strong class="text-slate-400">Every 30 min</strong> — 5-hour-before reminder fires when flight is 5h away.</p><p class="text-slate-500 text-sm">🌙 <strong class="text-red-400">8:00 PM UTC</strong> — If driver not assigned for tomorrow: pilgrim notified, admin alerted. <em class="text-slate-600">Auto safety net.</em></p></div>`,1)]),j.value?.dates?.length?m(``,!0):(i(),s(`div`,Pe,[a[25]||=h(`div`,{class:`text-5xl mb-3`},`📆`,-1),h(`p`,null,`No upcoming travel in the next `+o(M.value)+` days.`,1)])),(i(!0),s(u,null,d(j.value?.dates,e=>(i(),s(`div`,{key:e.date,class:`card space-y-4`},[h(`div`,_,[h(`h3`,v,`📅 `+o(e.dayLabel),1),h(`div`,y,[e.arrivingCount?(i(),s(`span`,Fe,` 🛬 `+o(e.arrivingCount)+` arriving `,1)):m(``,!0),e.departingCount?(i(),s(`span`,Ie,` 🛫 `+o(e.departingCount)+` departing `,1)):m(``,!0)])]),e.arrivals?.length?(i(),s(`div`,Le,[a[26]||=h(`p`,{class:`text-green-400 text-xs font-semibold uppercase tracking-wider mb-2`},`🛬 Arrivals`,-1),c(x,{rows:e.arrivals,statusColors:B,statusIcons:V,wa:G,onAssign:a[3]||=e=>Z(e,1)},null,8,[`rows`])])):m(``,!0),e.departures?.length?(i(),s(`div`,Re,[a[27]||=h(`p`,{class:`text-blue-400 text-xs font-semibold uppercase tracking-wider mb-2`},`🛫 Departures`,-1),c(x,{rows:e.departures,statusColors:B,statusIcons:V,wa:G,onAssign:a[4]||=e=>Z(e,2)},null,8,[`rows`])])):m(``,!0)]))),128))],64)):S.value===`arrivals`?(i(),s(u,{key:5},[h(`div`,ze,[a[28]||=h(`label`,{class:`text-slate-400 text-sm`},`Date:`,-1),r(h(`input`,{"onUpdate:modelValue":a[5]||=e=>N.value=e,type:`date`,class:`input w-auto`,onChange:q},null,544),[[f,N.value]])]),h(`div`,Be,[h(`h3`,Ve,[p(`🛬 Arrivals on `+o(U(N.value))+` `,1),h(`span`,He,`(`+o(k.value.length)+`)`,1)]),k.value.length?(i(),s(`div`,Ue,[c(g,{rows:k.value,type:1,statusColors:B,statusIcons:V,statusOptions:H,updatingId:z.value,wa:G,fmtDate:U,fmtTime:W,onAssign:Z,onStatus:Q,onRemove:$},null,8,[`rows`,`updatingId`])])):(i(),s(`div`,We,[...a[29]||=[h(`div`,{class:`text-5xl mb-3`},`🛬`,-1),h(`p`,null,`No arrivals on this date.`,-1)]]))])],64)):S.value===`departures`?(i(),s(u,{key:6},[h(`div`,Ge,[a[30]||=h(`label`,{class:`text-slate-400 text-sm`},`Date:`,-1),r(h(`input`,{"onUpdate:modelValue":a[6]||=e=>P.value=e,type:`date`,class:`input w-auto`,onChange:J},null,544),[[f,P.value]])]),h(`div`,Ke,[h(`h3`,qe,[p(`🛫 Departures on `+o(U(P.value))+` `,1),h(`span`,Je,`(`+o(A.value.length)+`)`,1)]),A.value.length?(i(),s(`div`,Ye,[c(g,{rows:A.value,type:2,statusColors:B,statusIcons:V,statusOptions:H,updatingId:z.value,wa:G,fmtDate:U,fmtTime:W,onAssign:Z,onStatus:Q,onRemove:$},null,8,[`rows`,`updatingId`])])):(i(),s(`div`,Xe,[...a[31]||=[h(`div`,{class:`text-5xl mb-3`},`🛫`,-1),h(`p`,null,`No departures on this date.`,-1)]]))])],64)):m(``,!0),(i(),ne(te,{to:`body`},[F.value?(i(),s(`div`,Ze,[h(`div`,Qe,[h(`div`,$e,[h(`div`,null,[h(`h3`,et,o(L.value===1?`🛬 Assign Pickup Driver`:`🛫 Assign Dropoff Driver`),1),h(`p`,tt,[p(o(I.value?.fullName)+` — `+o(I.value?.flightNumber||`No flight set`)+` `,1),I.value?.airport?(i(),s(`span`,nt,` · `+o(I.value.airport),1)):m(``,!0)])]),h(`button`,{onClick:a[7]||=e=>F.value=!1,class:`text-slate-500 hover:text-white text-xl`},`✕`)]),h(`div`,rt,[h(`div`,it,[h(`div`,null,[a[32]||=h(`label`,{class:`label`},`Driver Name *`,-1),r(h(`input`,{"onUpdate:modelValue":a[8]||=e=>R.value.driverName=e,type:`text`,class:`input`,placeholder:`Full name`},null,512),[[f,R.value.driverName]])]),h(`div`,null,[a[33]||=h(`label`,{class:`label`},`Driver Phone`,-1),r(h(`input`,{"onUpdate:modelValue":a[9]||=e=>R.value.driverPhone=e,type:`text`,class:`input`,placeholder:`+964...`},null,512),[[f,R.value.driverPhone]])])]),h(`div`,b,[h(`div`,null,[a[34]||=h(`label`,{class:`label`},`Vehicle Number`,-1),r(h(`input`,{"onUpdate:modelValue":a[10]||=e=>R.value.vehicleNumber=e,type:`text`,class:`input`,placeholder:`BAS-1234`},null,512),[[f,R.value.vehicleNumber]])]),h(`div`,null,[a[35]||=h(`label`,{class:`label`},`Vehicle Type`,-1),r(h(`select`,{"onUpdate:modelValue":a[11]||=e=>R.value.vehicleType=e,class:`input`},[(i(),s(u,null,d(ut,e=>h(`option`,{key:e,value:e},o(e),9,at)),64))],512),[[l,R.value.vehicleType]])])]),h(`div`,null,[h(`label`,ot,o(L.value===1?`Meeting Point at Airport`:`Pickup Location`),1),r(h(`input`,{"onUpdate:modelValue":a[12]||=e=>R.value.meetingPoint=e,type:`text`,class:`input`,placeholder:L.value===1?`Arrivals Hall Gate 2`:`Hotel lobby / room number`},null,8,st),[[f,R.value.meetingPoint]])]),h(`div`,null,[a[36]||=h(`label`,{class:`label`},`Scheduled Time`,-1),r(h(`input`,{"onUpdate:modelValue":a[13]||=e=>R.value.scheduledTime=e,type:`datetime-local`,class:`input`},null,512),[[f,R.value.scheduledTime]])]),h(`div`,null,[a[37]||=h(`label`,{class:`label`},`Notes`,-1),r(h(`textarea`,{"onUpdate:modelValue":a[14]||=e=>R.value.notes=e,class:`input h-20 resize-none`,placeholder:`Special instructions...`},null,512),[[f,R.value.notes]])])]),h(`div`,ct,[h(`button`,{onClick:pt,disabled:w.value||!R.value.driverName,class:`btn-primary flex-1 disabled:opacity-50`},o(w.value?`Saving...`:I.value?.transfer?`Update Assignment`:`Assign Driver`),9,lt),h(`button`,{onClick:a[15]||=e=>F.value=!1,class:`px-4 py-2 text-slate-400 hover:text-white text-sm border border-dark-600 rounded-xl`},`Cancel`)])])])):m(``,!0)]))])}}});export{x as default};