import React, { useState, useEffect } from 'react';
import { Sidebar } from './components/Sidebar';
import { TopBar } from './components/TopBar';
import { OverviewView } from './components/views/OverviewView';
import { RosterView } from './components/views/RosterView';
import { FamilyTreeView } from './components/views/FamilyTreeView';
import { TasksView } from './components/views/TasksView';
import { ExpeditionsView } from './components/views/ExpeditionsView';
import { CombatView } from './components/views/CombatView';
import { FacilitiesView } from './components/views/FacilitiesView';
import { WorldView } from './components/views/WorldView';
import { InventoryView } from './components/views/InventoryView';
import { AlchemyView } from './components/views/AlchemyView';
import { ForgeView } from './components/views/ForgeView';
import { ManualsView } from './components/views/ManualsView';
import { BeastsView } from './components/views/BeastsView';
import { MirrorView } from './components/views/MirrorView';
import { DestinyView } from './components/views/DestinyView';
import { MarketView } from './components/views/MarketView';
import { TutorialModal } from './components/TutorialModal';
import { mockClan, mockEvents, mockExpeditions } from './data/mockData';
import { CombatEngine } from './engine/CombatEngine';
import { Play } from 'lucide-react';
import { TaskType, Character, GameEvent } from './types/game';
import { CombatState } from './types/combat';
import { motion, AnimatePresence } from 'motion/react';

import { SaveSystem } from './engine/systems/SaveSystem';

import { GameManager } from './engine/core/GameManager';

export default function App() {
  const [currentView, setCurrentView] = useState('overview');
  const [activeCombat, setActiveCombat] = useState<CombatState | null>(null);
  const [notifications, setNotifications] = useState<{ id: string; message: string; type: 'success' | 'error' | 'info' }[]>([]);
  const [showTutorial, setShowTutorial] = useState(() => !localStorage.getItem('tutorial_seen'));
  const [showResetConfirm, setShowResetConfirm] = useState(false);
  
  const [clan, setClan] = useState(() => {
    const loadedClan = SaveSystem.loadClan();
    const loadedEvents = SaveSystem.loadEvents();
    GameManager.getInstance().initialize(loadedClan, loadedEvents);
    return loadedClan;
  });
  const [events, setEvents] = useState(() => SaveSystem.loadEvents());

  // Keep GameManager in sync with React state if it changes outside of GameManager
  useEffect(() => {
    GameManager.getInstance().initialize(clan, events);
  }, [clan, events]);

  const [isDarkMode, setIsDarkMode] = useState(() => localStorage.getItem('theme') === 'dark');

  useEffect(() => {
    if (isDarkMode) {
      document.documentElement.classList.add('dark');
      localStorage.setItem('theme', 'dark');
    } else {
      document.documentElement.classList.remove('dark');
      localStorage.setItem('theme', 'light');
    }
  }, [isDarkMode]);

  const toggleDarkMode = () => setIsDarkMode(!isDarkMode);

  useEffect(() => {
    SaveSystem.save(clan, events);
  }, [clan, events]);

  const handleReset = () => {
    setShowResetConfirm(true);
  };

  const confirmReset = () => {
    SaveSystem.reset();
    window.location.reload();
  };

  const handleExportSave = () => {
    SaveSystem.exportSave(clan, events);
    addNotification('Sauvegarde exportée avec succès', 'success');
  };

  const handleImportSave = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const content = e.target?.result as string;
        const saveData = JSON.parse(content);
        if (saveData.clan && saveData.events) {
          GameManager.getInstance().initialize(saveData.clan, saveData.events);
          setClan(saveData.clan);
          setEvents(saveData.events);
          addNotification('Sauvegarde importée avec succès', 'success');
        } else {
          addNotification('Fichier de sauvegarde invalide', 'error');
        }
      } catch (error) {
        addNotification('Erreur lors de la lecture du fichier', 'error');
      }
    };
    reader.readAsText(file);
    event.target.value = '';
  };

  const addNotification = (message: string, type: 'success' | 'error' | 'info' = 'info') => {
    const id = Math.random().toString(36).substr(2, 9);
    setNotifications(prev => [...prev, { id, message, type }]);
    setTimeout(() => {
      setNotifications(prev => prev.filter(n => n.id !== id));
    }, 5000);
  };

  const [isAdvancing, setIsAdvancing] = useState(false);

  const handleAdvanceYears = async (years: number) => {
    if (isAdvancing) return;
    setIsAdvancing(true);
    
    const gm = GameManager.getInstance();

    for (let i = 0; i < years; i++) {
      const { newClan, newEvents } = await gm.advanceYear();
      
      setClan(newClan);
      setEvents(newEvents);
      
      if (years > 1 && i < years - 1) {
        await new Promise(resolve => setTimeout(resolve, 1500));
      }
    }
    
    if (years === 1) {
      addNotification(`L'année ${gm.clanManager?.currentYear} a commencé.`, 'info');
    } else {
      addNotification(`${years} années se sont écoulées.`, 'info');
    }
    
    setIsAdvancing(false);
  };

  const handleAutoAssign = () => {
    const gm = GameManager.getInstance();
    if (gm.clanManager) {
      gm.clanManager.autoAssignTasks();
      setClan(gm.clanManager.toData());
      addNotification('Tâches auto-assignées avec succès', 'success');
    }
  };

  const handleUpdateTask = (memberId: string, task: TaskType, manualId?: string) => {
    const gm = GameManager.getInstance();
    if (gm.clanManager) {
      gm.clanManager.updateTask(memberId, task, manualId);
      setClan(gm.clanManager.toData());
      addNotification(`Tâche mise à jour pour ${gm.clanManager.bloodRegistry.getMember(memberId)?.firstName}`, 'info');
    }
  };

  const handleDispatchExpedition = (expeditionId: string, leaderId: string, memberIds: string[]) => {
    const expedition = mockExpeditions.find(e => e.id === expeditionId);
    const leader = clan.members.find(m => m.id === leaderId);
    const members = memberIds
      .filter(id => id !== leaderId)
      .map(id => clan.members.find(m => m.id === id))
      .filter(Boolean) as Character[];
    
    if (expedition && leader) {
      const combatState = CombatEngine.initializeCombat(clan, expedition, leader, members);
      setActiveCombat(combatState);
      addNotification(`L'expédition "${expedition.name}" commence !`, 'info');
    }
  };

  const handleCombatEnd = (result: 'victory' | 'defeat', finalState: CombatState) => {
    const { newClan, event } = GameManager.getInstance().resolveCombat(finalState);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : 'error');
    setActiveCombat(null);
  };

  const handlePurchaseDestinyUpgrade = (upgradeId: string) => {
    import('./constants').then(({ DESTINY_UPGRADES }) => {
      const upgrade = DESTINY_UPGRADES.find(u => u.id === upgradeId);
      if (!upgrade || clan.destiny < upgrade.cost || clan.destinyUpgrades?.includes(upgradeId)) return;

      const newClan = {
        ...clan,
        destiny: clan.destiny - upgrade.cost,
        destinyUpgrades: [...(clan.destinyUpgrades || []), upgradeId]
      };

      const event: GameEvent = {
        id: Math.random().toString(36).substr(2, 9),
        year: clan.currentYear,
        type: 'Success',
        message: `Le clan a débloqué la bénédiction de la destinée : ${upgrade.name}.`
      };

      setClan(newClan);
      setEvents(prev => [event, ...prev].slice(0, 50));
      addNotification(event.message, 'success');
    });
  };

  const handleBuyMarketItem = (itemId: string) => {
    const { newClan, event } = GameManager.getInstance().buyMarketItem(itemId);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : 'error');
  };

  const handleUpgradeFacility = (facilityId: string) => {
    const facility = clan.facilities.find(f => f.id === facilityId);
    if (!facility || clan.wealth < facility.cost) return;

    const updatedFacilities = clan.facilities.map(f => {
      if (f.id === facilityId) {
        return {
          ...f,
          level: f.level + 1,
          cost: Math.floor(f.cost * 1.8),
          bonus: f.type === 'Wealth' ? f.bonus + 200 : f.bonus + 5
        };
      }
      return f;
    });

    setClan({
      ...clan,
      wealth: clan.wealth - facility.cost,
      facilities: updatedFacilities
    });

    setEvents(prev => [{
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type: 'Success',
      message: `Infrastructure améliorée : ${facility.name} est maintenant niveau ${facility.level + 1}.`
    }, ...prev].slice(0, 50));
    addNotification(`${facility.name} amélioré au niveau ${facility.level + 1}`, 'success');
  };

  const handleUseItem = (memberId: string, itemId: string) => {
    const item = clan.inventory.find(i => i.id === itemId);
    if (!item || item.quantity <= 0) return;

    const updatedMembers = clan.members.map(m => {
      if (m.id !== memberId) return m;
      const updated = { ...m };
      if (item.effect.type === 'Breakthrough') {
        updated.breakthroughProgress = Math.min(100, updated.breakthroughProgress + item.effect.value);
        updated.mentalStability = Math.min(100, updated.mentalStability + 5);
      } else if (item.effect.type === 'Lifespan') {
        updated.maxLifespan += item.effect.value;
      }
      return updated;
    });

    const updatedInventory = clan.inventory.map(i => 
      i.id === itemId ? { ...i, quantity: i.quantity - 1 } : i
    ).filter(i => i.quantity > 0);

    setClan({
      ...clan,
      members: updatedMembers,
      inventory: updatedInventory
    });

    setEvents(prev => [{
      id: Math.random().toString(36).substr(2, 9),
      year: clan.currentYear,
      type: 'Info',
      message: `Objet utilisé : ${item.name} sur ${clan.members.find(m => m.id === memberId)?.firstName}.`
    }, ...prev].slice(0, 50));
    addNotification(`${item.name} utilisé avec succès`, 'success');
  };

  const handleInteractSect = (sectId: string, action: 'Diplomacy' | 'Tribute' | 'Challenge') => {
    const { newClan, event } = GameManager.getInstance().interactWithSect(sectId, action);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : event.type === 'Danger' ? 'error' : 'info');
  };

  const handleCraft = (recipeId: string, alchemistId: string) => {
    const { newClan, event } = GameManager.getInstance().craftItem(recipeId, alchemistId);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : 'error');
  };

  const handleForge = (recipeId: string, smithId: string) => {
    const { newClan, event } = GameManager.getInstance().forgeArtifact(recipeId, smithId);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : 'error');
  };

  const handleUseDivinePower = async (powerId: string, targetId: string) => {
    const { newClan, event } = await GameManager.getInstance().useDivinePower(powerId, targetId);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : 'error');
  };

  const handleArrangeMarriage = (memberId: string) => {
    const { newClan, event } = GameManager.getInstance().arrangeMarriage(memberId);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : 'error');
  };

  const handleDeduceManual = (manualId1: string, manualId2: string) => {
    const { newClan, event } = GameManager.getInstance().deduceManual(manualId1, manualId2);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : 'error');
  };

  const handleFeedBeast = (beastId: string) => {
    const { newClan, event } = GameManager.getInstance().feedBeast(beastId);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : 'error');
  };

  const handleAssignBeast = (beastId: string, memberId?: string) => {
    const { newClan, event } = GameManager.getInstance().assignBeast(beastId, memberId);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, 'success');
  };

  const handleDeduceFromFragments = (fragmentIds: string[]) => {
    const { newClan, event } = GameManager.getInstance().deduceFromFragments(fragmentIds);
    setClan(newClan);
    setEvents(prev => [event, ...prev].slice(0, 50));
    addNotification(event.message, event.type === 'Success' ? 'success' : 'error');
  };

  const renderView = () => {
    switch (currentView) {
      case 'overview':
        return <OverviewView clan={clan} events={events} onAdvanceYears={handleAdvanceYears} isAdvancing={isAdvancing} />;
      case 'roster':
        return <RosterView members={clan.members} onUseItem={handleUseItem} onUpdateTask={handleUpdateTask} onUseDivinePower={handleUseDivinePower} onArrangeMarriage={handleArrangeMarriage} inventory={clan.inventory} manuals={clan.manuals} mirrorPower={clan.mirrorPower} />;
      case 'family-tree':
        return <FamilyTreeView members={clan.members} onArrangeMarriage={handleArrangeMarriage} />;
      case 'facilities':
        return <FacilitiesView clan={clan} onUpgrade={handleUpgradeFacility} />;
      case 'inventory':
        return <InventoryView clan={clan} onUseItem={handleUseItem} />;
      case 'world':
        return <WorldView clan={clan} events={events} onInteract={handleInteractSect} onUseDivinePower={handleUseDivinePower} />;
      case 'alchemy':
        return <AlchemyView clan={clan} onCraft={handleCraft} />;
      case 'forge':
        return <ForgeView clan={clan} onForge={handleForge} />;
      case 'manuals':
        return <ManualsView clan={clan} onDeduceManual={handleDeduceManual} />;
      case 'beasts':
        return <BeastsView clan={clan} onFeedBeast={handleFeedBeast} onAssignBeast={handleAssignBeast} />;
      case 'mirror':
        return <MirrorView clan={clan} onUseDivinePower={handleUseDivinePower} onDeduceFromFragments={handleDeduceFromFragments} />;
      case 'tasks':
        return <TasksView members={clan.members} manuals={clan.manuals} onAutoAssign={handleAutoAssign} onUpdateTask={handleUpdateTask} />;
      case 'combat':
        return <ExpeditionsView clan={clan} events={events} onDispatch={handleDispatchExpedition} />;
      case 'destiny':
        return <DestinyView clan={clan} onPurchaseUpgrade={handlePurchaseDestinyUpgrade} />;
      case 'market':
        return <MarketView clan={clan} onBuyItem={handleBuyMarketItem} />;
      default:
        return <OverviewView clan={clan} events={events} onAdvanceYears={handleAdvanceYears} isAdvancing={isAdvancing} />;
    }
  };

  const handleCloseTutorial = () => {
    localStorage.setItem('tutorial_seen', 'true');
    setShowTutorial(false);
  };

  return (
    <div className="flex h-screen bg-ink text-paper overflow-hidden font-sans selection:bg-gold-500/30">
      {activeCombat ? (
        <div className="flex-1 w-full h-full z-50 bg-ink">
          <CombatView initialState={activeCombat} onCombatEnd={handleCombatEnd} />
        </div>
      ) : (
        <>
          <Sidebar 
            currentView={currentView} 
            setCurrentView={setCurrentView} 
            clan={clan} 
            onReset={handleReset}
            isDarkMode={isDarkMode}
            toggleDarkMode={toggleDarkMode}
            onExport={handleExportSave}
            onImport={handleImportSave}
          />
          
          <div className="flex-1 flex flex-col h-full overflow-hidden relative">
            {/* Subtle background glow effect */}
            <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[400px] bg-gold-500/5 blur-[120px] rounded-full pointer-events-none" />
            
            <TopBar clan={clan} />
            
            <main className="flex-1 m-4 relative z-0">
              <div className="absolute inset-0 bg-[#0a0a0a] border border-amber-900/20 shadow-inner rounded-2xl pointer-events-none overflow-hidden">
                <div className="absolute inset-0 bg-[url('https://www.transparenttextures.com/patterns/rice-paper.png')] opacity-5 mix-blend-overlay" />
              </div>
              <div className="absolute inset-0 overflow-y-auto custom-scrollbar rounded-2xl z-10">
                <AnimatePresence mode="wait">
                  <motion.div
                    key={currentView}
                    initial={{ opacity: 0, x: 20 }}
                    animate={{ opacity: 1, x: 0 }}
                    exit={{ opacity: 0, x: -20 }}
                    transition={{ duration: 0.3, ease: "easeOut" }}
                    className="min-h-full"
                  >
                    {renderView()}
                  </motion.div>
                </AnimatePresence>
              </div>
            </main>

            {/* Tutorial Modal */}
            {showTutorial && <TutorialModal onClose={handleCloseTutorial} />}

            {/* Reset Confirmation Modal */}
            <AnimatePresence>
              {showResetConfirm && (
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="fixed inset-0 z-[200] flex items-center justify-center bg-ink/80 backdrop-blur-sm"
                >
                  <motion.div
                    initial={{ scale: 0.9, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    exit={{ scale: 0.9, opacity: 0 }}
                    className="paper-card p-8 max-w-md w-full mx-4 space-y-6 relative overflow-hidden"
                  >
                    <div className="absolute inset-0 bagua-bg opacity-10 pointer-events-none" />
                    <h3 className="text-2xl font-calligraphy text-cinnabar relative z-10">Réinitialiser la progression ?</h3>
                    <p className="text-sm opacity-80 font-serif relative z-10">
                      Êtes-vous sûr de vouloir effacer toute votre progression ? Cette action est irréversible et votre clan sera perdu à jamais dans les méandres du temps.
                    </p>
                    <div className="flex justify-end gap-4 pt-4 relative z-10">
                      <button
                        onClick={() => setShowResetConfirm(false)}
                        className="px-4 py-2 rounded-lg border border-paper/20 hover:bg-paper/5 transition-colors text-sm font-bold uppercase tracking-widest"
                      >
                        Annuler
                      </button>
                      <button
                        onClick={confirmReset}
                        className="px-4 py-2 rounded-lg bg-cinnabar/20 text-cinnabar border border-cinnabar/50 hover:bg-cinnabar hover:text-white transition-colors text-sm font-bold uppercase tracking-widest shadow-[0_0_15px_rgba(239,68,68,0.3)]"
                      >
                        Confirmer
                      </button>
                    </div>
                  </motion.div>
                </motion.div>
              )}
            </AnimatePresence>

            {/* Notifications */}
            <div className="fixed top-4 right-4 z-[100] space-y-3 pointer-events-none">
              <AnimatePresence>
                {notifications.map(n => (
                  <motion.div 
                    key={n.id} 
                    initial={{ opacity: 0, x: 100, scale: 0.9 }}
                    animate={{ opacity: 1, x: 0, scale: 1 }}
                    exit={{ opacity: 0, x: 100, scale: 0.9 }}
                    className={`pointer-events-auto px-6 py-4 rounded-2xl shadow-2xl border backdrop-blur-md flex items-center gap-4 paper-card ${
                      n.type === 'success' ? 'border-jade-500/30' :
                      n.type === 'error' ? 'border-cinnabar/30' :
                      'border-paper/10'
                    }`}
                  >
                    <div className={`w-3 h-3 rounded-full animate-pulse ${
                      n.type === 'success' ? 'bg-jade-500 shadow-[0_0_10px_rgba(16,185,129,0.5)]' :
                      n.type === 'error' ? 'bg-cinnabar shadow-[0_0_10px_rgba(220,38,38,0.5)]' :
                      'bg-gold-500 shadow-[0_0_10px_rgba(251,191,36,0.5)]'
                    }`} />
                    <p className={`text-sm font-serif font-bold ${
                      n.type === 'success' ? 'text-jade-400' :
                      n.type === 'error' ? 'text-cinnabar' :
                      'text-paper'
                    }`}>{n.message}</p>
                  </motion.div>
                ))}
              </AnimatePresence>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
