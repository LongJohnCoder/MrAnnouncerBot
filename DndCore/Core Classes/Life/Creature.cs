﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using GoogleHelper;

namespace DndCore
{
	public abstract class Creature
	{
		public event MessageEventHandler RequestMessageToDungeonMaster;

		public event ConditionsChangedEventHandler ConditionsChanged;
		protected virtual void OnConditionsChanged(object sender, ConditionsChangedEventArgs ea)
		{
			ConditionsChanged?.Invoke(sender, ea);
		}

		const string STR_Absolute = ".Absolute";
		const string STR_AddAbilityModifier = ".AddAbilityModifier";
		const string STR_Advantage = ".Advantage";
		const string STR_Disadvantage = ".Disadvantage";
		const string STR_LimitAbilityModifier = ".LimitAbilityModifier";
		const string STR_Multiplier = ".Multiplier";

		private Conditions activeConditions { get; set; } = Conditions.None;
		public Against advantages { get; set; } = Against.none;
		public string alignmentStr = string.Empty;
		public Alignment Alignment { get; set; }
		public string race { get; set; }

		[JsonIgnore]
		public List<Attack> attacks = new List<Attack>();

		[JsonIgnore]
		public Vector Location { get; protected set; }

		public void SetLocation(Vector location)
		{
			Location = location;
		}

		public double baseArmorClass = 0;
		public double tempArmorClassMod = 0;
		public double baseCharisma;

		public double baseConstitution;

		public double baseDexterity;
		public double baseIntelligence;

		public double baseStrength;
		public double baseWisdom;

		// TODO: May want to make this private after removing dependencies to this variable name due to JSON serialization in TS files.
		public double baseWalkingSpeed = 0;
		public double burrowingSpeed { get; set; } = 0;

		[JsonIgnore]
		Dictionary<string, double> calculatedMods = new Dictionary<string, double>();
		public double climbingSpeed = 0;
		public Conditions conditionImmunities = Conditions.None;
		public CreatureSize creatureSize = CreatureSize.Medium;

		[JsonIgnore]
		public ObservableCollection<CurseBlessingDisease> cursesAndBlessings = new ObservableCollection<CurseBlessingDisease>();
		[JsonIgnore]
		public ObservableCollection<DamageFilter> damageImmunities = new ObservableCollection<DamageFilter>();
		[JsonIgnore]
		public ObservableCollection<DamageFilter> damageResistance = new ObservableCollection<DamageFilter>();
		[JsonIgnore]
		public ObservableCollection<DamageFilter> damageVulnerability = new ObservableCollection<DamageFilter>();

		public virtual double darkvisionRadius { get; set; } = 0;
		public virtual double tremorSenseRadius { get; set; } = 0;
		public virtual double truesightRadius { get; set; } = 0;
		public virtual double blindsightRadius { get; set; } = 0;

		public Against disadvantages { get; set; } = Against.none;
		public ObservableCollection<ItemViewModel> equipment = new ObservableCollection<ItemViewModel>();

		public double flyingSpeed { get; set; } = 0;

		[Column]
		public decimal goldPieces = 0;

		public decimal GoldPieces
		{
			get { return goldPieces; }
			set
			{
				goldPieces = value;
			}
		}


		[Column]
		public double hitPoints = 0;


		public double HitPoints
		{
			get { return hitPoints; }
			set
			{
				hitPoints = value;
			}
		}


		public double initiative = 0;
		public CreatureKinds kind = CreatureKinds.None;
		public Languages languagesSpoken = Languages.None;
		public Languages languagesUnderstood = Languages.None;
		public double maxHitPoints { get; set; } = 0;

		[JsonIgnore]
		public List<Attack> multiAttack = new List<Attack>();

		[JsonIgnore]
		public MultiAttackCount multiAttackCount = MultiAttackCount.oneEach;

		public string name = string.Empty;

		[JsonIgnore]
		public string Name { get => name; set => name = value; }


		[JsonIgnore]
		public bool IsSelected { get; set; }

		protected bool needToRecalculateMods;
		public int offTurnActions = 0;
		public int onTurnActions = 1;
		
		public Senses senses { get; set; } = Senses.Hearing | Senses.NormalVision | Senses.Smell;
		public double swimmingSpeed { get; set; } = 0;
		public double telepathyRadius { get; set; } = 0; // feet
		public double tempHitPoints = 0;
		
		public Creature()
		{
			equipment.CollectionChanged += ItemCollectionChanged;
			cursesAndBlessings.CollectionChanged += ItemCollectionChanged;
			damageImmunities.CollectionChanged += ItemCollectionChanged;
			damageResistance.CollectionChanged += ItemCollectionChanged;
			damageVulnerability.CollectionChanged += ItemCollectionChanged;
		}

		public double ArmorClass
		{
			get
			{
				double absolute = GetAbsolute();
				double thisArmorClass = baseArmorClass;
				if (absolute > 0)
					thisArmorClass = absolute;
				return thisArmorClass + tempArmorClassMod + GetMods();
			}
		}

		public Dictionary<string, double> CalculatedMods
		{
			get
			{
				RecalculateModsIfNecessary();
				return calculatedMods;
			}
			set => calculatedMods = value;
		}
		
		[JsonIgnore]
		public Dictionary<string, Dictionary<string, PropertyMod>> PropertyMods { get; set; } = new Dictionary<string, Dictionary<string, PropertyMod>>();

		[JsonIgnore]
		public Dictionary<string, VantageMod> VantageMods { get; set; } = new Dictionary<string, VantageMod>();

		public virtual double Strength
		{
			get
			{
				return baseStrength + GetMods(Ability.strength);
			}
		}
		public virtual double Wisdom
		{
			get
			{
				return baseWisdom + GetMods(Ability.wisdom);
			}

		}


		public virtual double Charisma
		{
			get
			{
				return baseCharisma + GetMods(Ability.charisma);
			}
		}
		public virtual double Constitution
		{
			get
			{
				return baseConstitution + GetMods(Ability.constitution);
			}

		}


		public virtual double Dexterity
		{
			get
			{
				return baseDexterity + GetMods(Ability.dexterity);
			}
		}

		[JsonIgnore]
		public DndGame Game { get; set; }

		public double Intelligence
		{
			get
			{
				return baseIntelligence + GetMods(Ability.intelligence);
			}

		}
		public double LastDamagePointsTaken { get; protected set; }

		public DamageType LastDamageTaken { get; protected set; }
		public double WalkingSpeed
		{
			get
			{
				return CalculateProperty(baseWalkingSpeed);
			}	
			set
			{
				baseWalkingSpeed = GetBasePropertyValue(value);
			}
		}

		//    [ ] Tooltip to return an explanation of mods (one per line)
		//        "-10 due to Ray of Frost (expires at end of Merkin's next turn)."
		string GetModExplanation(string propertyName)
		{
			return string.Empty;
		}

		public Conditions ActiveConditions
		{
			get => activeConditions;
			set
			{
				if (activeConditions == value)
					return;

				Conditions oldConditions = activeConditions;

				activeConditions = value;

				OnConditionsChanged(this, new ConditionsChangedEventArgs(oldConditions, activeConditions));
			}
		}

		public void AddDamageImmunity(DamageType damageType, AttackKind attackKind = AttackKind.Any)
		{
			damageImmunities.Add(new DamageFilter(damageType, attackKind));
		}

		public void AddDamageResistance(DamageType damageType, AttackKind attackKind)
		{
			damageResistance.Add(new DamageFilter(damageType, attackKind));
		}

		public void AddDamageVulnerability(DamageType damageType, AttackKind attackKind = AttackKind.Any)
		{
			damageVulnerability.Add(new DamageFilter(damageType, attackKind));
		}

		void AddCalculatedMod(string key, double value)
		{
			if (calculatedMods.ContainsKey(key))
				CalculatedMods[key] += value;
			else
				CalculatedMods.Add(key, value);
		}

		int CalculateAbilityModifier(double abilityScore)
		{
			return (int)Math.Floor((abilityScore - 10) / 2.0);
		}

		void CalculateMods(ModViewModel mod, bool equipped)
		{
			if (mod.RequiresConsumption)
				return;

			if (mod.RequiresEquipped && !equipped)
				return;

			if (!string.IsNullOrWhiteSpace(mod.TargetName) && mod.TargetName != "None")
			{
				if (mod.Offset > 0)
				{
					AddCalculatedMod(mod.TargetName, mod.Offset);
				}
				else if (mod.Multiplier != 1)
				{
					AddCalculatedMod(mod.TargetName + STR_Multiplier, mod.Multiplier);
				}
				else if (mod.Absolute != 0)
				{
					AddCalculatedMod(mod.TargetName + STR_Absolute, mod.Absolute);
				}

				if (mod.AddAbilityModifier != Ability.none)
				{
					AddCalculatedMod(mod.TargetName + STR_AddAbilityModifier, (int)mod.AddAbilityModifier);
					AddCalculatedMod(mod.TargetName + STR_LimitAbilityModifier, mod.ModifierLimit);
				}
			}

			if (mod.AddsAdvantage)
			{
				AddCalculatedMod(mod.VantageSkillFilter.ToString() + STR_Advantage, 1);
			}
			else if (mod.AddsDisadvantage)
			{
				AddCalculatedMod(mod.VantageSkillFilter.ToString() + STR_Disadvantage, 1);
			}
		}

		void CalculateModsForItem(ItemViewModel itemViewModel)
		{
			foreach (ModViewModel modViewModel in itemViewModel.mods)
				CalculateMods(modViewModel, itemViewModel.equipped);
		}

		public bool CanBeAffectedBy(Attack attack)
		{
			foreach (Damage damage in attack.damages)
			{
				bool canBeAffectedByTheAttack = false;
				if (damage.DamageType == DamageType.Condition && CanBeAffectedBy(damage.Conditions))
					canBeAffectedByTheAttack = true;
				else if (damage.DamageType != DamageType.None && damage.DamageType != DamageType.Condition && !IsImmuneTo(damage.DamageType, damage.AttackKind))
					canBeAffectedByTheAttack = true;

				if (canBeAffectedByTheAttack)
				{
					if (damage.IncludeCreatures != CreatureKinds.None || damage.IncludeTargetSenses != Senses.None || damage.IncludeCreatureSizes != CreatureSize.None)
					{
						bool missesCreatureKind = damage.IncludeCreatures != CreatureKinds.None && !MatchesCreatureKind(damage.IncludeCreatures);
						bool missesSense = damage.IncludeTargetSenses != Senses.None && !HasSense(damage.IncludeTargetSenses);
						bool missesCreatureSize = damage.IncludeCreatureSizes != CreatureSize.None && !MatchesSize(damage.IncludeCreatureSizes);

						bool attackHitsCreature = !(missesCreatureKind || missesSense || missesCreatureSize);

						if (attackHitsCreature)
							return true;
					}
				}
			}

			return false;
		}

		bool CanBeAffectedBy(Conditions conditions)
		{
			if (conditions == Conditions.None)
				return false;

			return !IsImmuneTo(conditions);
		}

		public void Equip(ItemViewModel item)
		{
			ItemViewModel existingItem = equipment.FirstOrDefault(x => x == item);
			if (existingItem == null)
				equipment.Add(item);
			item.equipped = true;
			needToRecalculateMods = true;
		}

		bool FilterCatches(ObservableCollection<DamageFilter> filter, DamageType damageType, AttackKind attackKind)
		{
			foreach (DamageFilter damageFilter in filter)
				if (damageFilter.Matches(damageType, attackKind))
					return true;
			return false;
		}

		double GetAbsolute([CallerMemberName] string key = null)
		{
			if (key == null)
				return 0d;
			RecalculateModsIfNecessary();
			return GetAbsoluteMod(key);
		}

		double GetAbsoluteMod(object key)
		{
			string keyStr = key.ToString() + STR_Absolute;
			if (CalculatedMods.ContainsKey(keyStr))
				return CalculatedMods[keyStr];
			return 0;
		}

		public virtual double GetAbilityModifier(Ability modifier)
		{
			switch (modifier)
			{
				case Ability.strength: return CalculateAbilityModifier(Strength);
				case Ability.dexterity: return CalculateAbilityModifier(Dexterity);
				case Ability.constitution: return CalculateAbilityModifier(Constitution);
				case Ability.intelligence: return CalculateAbilityModifier(Intelligence);
				case Ability.wisdom: return CalculateAbilityModifier(Wisdom);
				case Ability.charisma: return CalculateAbilityModifier(Charisma);
			}
			return 0;
		}

		public int GetAttackRoll(int basicRoll, Ability modifier)
		{
			return basicRoll + (int)Math.Floor(GetAbilityModifier(modifier));
		}

		double GetCalculatedMod(object key)
		{
			string keyStr = key.ToString();
			if (key is Ability)
			{
				keyStr = keyStr.InitialCap();
			}
			double abilityModifier = 0;

			if (CalculatedMods.ContainsKey(keyStr + STR_AddAbilityModifier))
			{
				Ability ability = (Ability)(int)CalculatedMods[keyStr + STR_AddAbilityModifier];
				abilityModifier = GetAbilityModifier(ability);
				double abilityModifierLimit = 0;
				if (CalculatedMods.ContainsKey(keyStr + STR_LimitAbilityModifier))
				{
					abilityModifierLimit = CalculatedMods[keyStr + STR_LimitAbilityModifier];
				}
				if (abilityModifierLimit != 0 && abilityModifier > abilityModifierLimit)
					abilityModifier = abilityModifierLimit;
			}
			if (CalculatedMods.ContainsKey(keyStr))
				return CalculatedMods[keyStr] + abilityModifier;
			return abilityModifier;
		}

		public double GetMods(Ability ability)
		{
			RecalculateModsIfNecessary();
			return GetCalculatedMod(ability);
		}

		double GetPropertyModMultiplier(string key)
		{
			if (!PropertyMods.ContainsKey(key))
				return 1;

			double result = 1;
			foreach (string id in PropertyMods[key].Keys)
				result *= PropertyMods[key][id].Multiplier;

			return result;
		}

		double GetPropertyModOffset(string key)
		{
			if (!PropertyMods.ContainsKey(key))
				return 0;

			double result = 0;
			foreach (string id in PropertyMods[key].Keys)
				result += PropertyMods[key][id].Offset;

			return result;
		}

		double CalculateProperty(double baseValue, double min = 0, double max = double.MaxValue, [CallerMemberName] string key = null)
		{
			return MathUtils.Clamp(GetPropertyModMultiplier(key) * (baseValue + GetPropertyModOffset(key) + GetMods(key)), min, max);
		}

		double GetBasePropertyValue(double setToValue, [CallerMemberName] string key = null)
		{
			return setToValue / GetPropertyModMultiplier(key) - (GetPropertyModOffset(key) + GetMods(key));
		}

		double GetMods([CallerMemberName] string key = null)
		{
			if (key == null)
				return 0d;
			RecalculateModsIfNecessary();
			return GetCalculatedMod(key);
		}

		public VantageKind GetSkillCheckDice(Skills skills)
		{
			string baseKey = skills.ToString();
			int vantageCount = 0;
			if (CalculatedMods.ContainsKey(baseKey + STR_Advantage))
			{
				vantageCount++;
			}
			else if (CalculatedMods.ContainsKey(baseKey + STR_Disadvantage))
			{
				vantageCount--;
			}

			if (vantageCount > 0)
				return VantageKind.Advantage;

			if (vantageCount < 0)
				return VantageKind.Disadvantage;

			return VantageKind.Normal;
		}

		public bool HasAnyCondition(Conditions conditions)
		{
			return (ActiveConditions & conditions) != 0;
		}

		public bool HasCondition(Conditions condition)
		{
			return (ActiveConditions & condition) == condition;
		}

		public bool HasSense(Senses sense)
		{
			if (sense == Senses.None)
				return false;

			if (HasAnyCondition(Conditions.Petrified | Conditions.Unconscious))
				return false;

			if (sense == Senses.NormalVision)
				return IsCapableOfSense(Senses.NormalVision) && !HasCondition(Conditions.Blinded);

			if (sense == Senses.Blindsight)
				return blindsightRadius > 0 && !HasCondition(Conditions.Blinded);

			if (sense == Senses.Darkvision)
				return darkvisionRadius > 0 && !HasCondition(Conditions.Blinded);

			if (sense == Senses.Hearing)
				return IsCapableOfSense(Senses.Hearing) && !HasCondition(Conditions.Deafened);

			if (sense == Senses.Truesight)
				return truesightRadius > 0 && !HasCondition(Conditions.Blinded);

			if (sense == Senses.Tremorsense)
				return tremorSenseRadius > 0;

			if (sense == Senses.Smell)
				return IsCapableOfSense(Senses.Smell);

			return false;
		}

		public bool IsCapableOfSense(Senses sense)
		{
			return (senses & sense) == sense;
		}

		public bool IsImmuneTo(Conditions conditions)
		{
			return (conditionImmunities & conditions) == conditions;
		}

		public bool IsImmuneTo(DamageType damageType, AttackKind attackKind)
		{
			return FilterCatches(damageImmunities, damageType, attackKind);
		}

		public bool IsResistantTo(DamageType damageType, AttackKind attackKind)
		{
			return FilterCatches(damageResistance, damageType, attackKind);
		}

		public bool IsVulnerableTo(DamageType damageType, AttackKind attackKind)
		{
			return FilterCatches(damageVulnerability, damageType, attackKind);
		}

		void ItemCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			needToRecalculateMods = true;
		}

		bool MatchesCreatureKind(CreatureKinds creatureKinds)
		{
			if (creatureKinds == CreatureKinds.None)
				return false;
			return creatureKinds.HasFlag(kind);
		}

		bool MatchesSize(CreatureSize size)
		{
			if (size == CreatureSize.None)
				return false;
			return size.HasFlag(creatureSize);
		}

		public void Pack(ItemViewModel item)
		{
			equipment.Add(item);
		}
		public void QueueAction(ActionAttack actionAttack)
		{
			Game.QueueAction(this, actionAttack);
		}

		void RecalculateModsIfNecessary()
		{
			if (!needToRecalculateMods)
				return;

			needToRecalculateMods = false;

			calculatedMods.Clear();
			foreach (ItemViewModel itemViewModel in equipment)
			{
				CalculateModsForItem(itemViewModel);
			}
		}

		public void TakeDamage(DamageType damageType, AttackKind attackKind, double points)
		{
			if (IsImmuneTo(damageType, attackKind))
			{
				LastDamageTaken = DamageType.None;
				LastDamagePointsTaken = 0;
				return;
			}
			double totalDamageTaken = points;

			if (IsResistantTo(damageType, attackKind))
				totalDamageTaken = DndUtils.HalveValue(totalDamageTaken);
			else if (IsVulnerableTo(damageType, attackKind))
				totalDamageTaken *= 2;

			LastDamageTaken = damageType;

			if (totalDamageTaken > HitPoints + tempHitPoints)  // Can only drop to zero HP
				totalDamageTaken = HitPoints + tempHitPoints;

			LastDamagePointsTaken = totalDamageTaken;

			if (totalDamageTaken == 0)
				return;

			double damageToInflict = totalDamageTaken;
			if (tempHitPoints > 0)
			{
				if (damageToInflict > tempHitPoints)
				{
					damageToInflict -= tempHitPoints;
					tempHitPoints = 0;
				}
				else
				{
					tempHitPoints -= damageToInflict;
					damageToInflict = 0;
				}
			}

			HitPoints -= damageToInflict;
			OnDamaged(totalDamageTaken);
		}

		public event StateChangedEventHandler StateChanged;
		public event CreatureDamagedEventHandler Damaged;

		protected virtual void OnDamaged(double damageAmount)
		{
			CreatureDamagedEventArgs ea = new CreatureDamagedEventArgs(this, damageAmount);
			Damaged?.Invoke(this, ea);
		}

		protected virtual void OnStateChanged(object sender, StateChangedEventArgs ea)
		{
			StateChanged?.Invoke(sender, ea);
		}

		protected void OnStateChanged(string key, object oldValue, object newValue, bool isRechargeable = false)
		{
			OnStateChanged(this, new StateChangedEventArgs(key, oldValue, newValue, isRechargeable));
		}

		public void Unequip(ItemViewModel item)
		{
			ItemViewModel firstItem = equipment.FirstOrDefault(x => x == item);
			if (firstItem == null)
				return;
			firstItem.equipped = false;
			needToRecalculateMods = true;
		}

		public void Unpack(ItemViewModel item, int count = 1)
		{
			int index = equipment.IndexOf(item);
			if (index >= 0)
			{
				ItemViewModel thisItem = equipment[index];
				if (thisItem.count > 0)
					if (count == int.MaxValue)
						thisItem.count = 0;
					else
						thisItem.count -= count;

				if (thisItem.count <= 0)
					equipment.RemoveAt(index);
			}
		}

		public void DieRollStoppedForTestCases(int score, int damage = 0)
		{
			DiceStoppedRollingData diceStoppedRollingData = new DiceStoppedRollingData();
			diceStoppedRollingData.damage = damage;
			Game.DieRollStopped(this, score, diceStoppedRollingData);
		}

		public virtual void ReadyRollDice(DiceRollType rollType, string diceStr, int hiddenThreshold = int.MinValue)
		{
			if (Game != null)
				Game.SetHiddenThreshold(this, hiddenThreshold, rollType);
		}

		public virtual void PrepareWeaponAttack(PlayerActionShortcut playerActionShortcut)
		{
			
		}

		public virtual Attack GetAttack(string attackName)
		{
			return null;
		}

		public void PrepareAttack(Creature creature, Attack attack)
		{
			if (Game != null)
			{
				Game.CreatureTakingAction(this);
				Game.CreaturePreparesAttack(this, creature, attack, false);
			}
		}

		public void PrepareAttack(Creature target, PlayerActionShortcut shortcut)
		{
			if (Game != null)
				Game.CreatureTakingAction(this);
			PrepareWeaponAttack(shortcut);
			if (Game != null)
				Game.CreaturePreparesAttack(this, target, null, shortcut.UsesMagic);
		}

		public virtual void Target(Creature target)
		{
			if (Game != null)
			{
				Game.CreatureTakingAction(this);
				Game.CreaturePreparesAttack(this, target, null, false);
			}
		}

		public virtual void StartTurnResetState()
		{
		}

		public virtual void EndTurnResetState()
		{
		}

		void AddCastedMagic(Magic magic)
		{
			castedMagic.Add(magic);
			magic.Dispel += CastedMagic_Dispelled;
		}

		[JsonIgnore]
		List<Magic> receivedMagic = new List<Magic>();

		[JsonIgnore]
		List<Magic> castedMagic = new List<Magic>();

		void ReceiveMagic(Magic magic)
		{
			magic.AddTarget(this);
			receivedMagic.Add(magic);
			magic.Dispel += ReceivedMagic_Dispelled;
			magic.TriggerOnReceived(this);
		}

		public void GiveMagic(string magicItemName, CastedSpell castedSpell, Target target, object data1, object data2, object data3, object data4, object data5, object data6, object data7, object data8)
		{
			Magic magic = new Magic(this, Game, magicItemName, castedSpell, data1, data2, data3, data4, data5, data6, data7, data8);
			if (target != null)
			{
				if (target.PlayerIds != null)
					foreach (int playerId in target.PlayerIds)
					{
						Character player = Game.GetPlayerFromId(playerId);
						if (player != null)
							player.ReceiveMagic(magic);
					}
				if (target.Creatures != null)
					foreach (Creature creature in target.Creatures)
					{
						if (creature != null)
						creature.ReceiveMagic(magic);
					}
			}

			AddCastedMagic(magic);
		}

		void RemoveReceivedMagic(Magic magic)
		{
			magic.Dispel -= ReceivedMagic_Dispelled;
			receivedMagic.Remove(magic);
		}

		void RemoveCastedMagic(Magic magic)
		{
			magic.Dispel -= CastedMagic_Dispelled;
			castedMagic.Remove(magic);
		}

		void ReceivedMagic_Dispelled(object sender, MagicEventArgs ea)
		{
			RemoveReceivedMagic(ea.Magic);
		}

		void CastedMagic_Dispelled(object sender, MagicEventArgs ea)
		{
			RemoveCastedMagic(ea.Magic);
		}

		public void AddPropertyMod(string propertyName, string id, double offset, double multiplier)
		{
			if (!PropertyMods.ContainsKey(propertyName))
				PropertyMods.Add(propertyName, new Dictionary<string, PropertyMod>());
			Dictionary<string, PropertyMod> propMods = PropertyMods[propertyName];
			if (!propMods.ContainsKey(id))
				propMods.Add(id, new PropertyMod(offset, multiplier));
			else
				propMods[id].Set(offset, multiplier);
		}
		
		public void RemovePropertyMod(string propertyName, string id)
		{
			if (!PropertyMods.ContainsKey(propertyName))
				return;
			Dictionary<string, PropertyMod> propMods = PropertyMods[propertyName];
			if (!propMods.ContainsKey(id))
				return;
			propMods.Remove(id);
		}

		public void AddVantageMod(string id, DiceRollType rollType, Skills skills, string dieLabel, int offset)
		{
			if (!VantageMods.ContainsKey(id))
				VantageMods.Add(id, new VantageMod(rollType, skills, dieLabel, offset));
			else
				VantageMods[id].Set(rollType, skills, dieLabel, offset);
		}

		protected virtual void OnRequestMessageToDungeonMaster(string message)
		{
			RequestMessageToDungeonMaster?.Invoke(this, new MessageEventArgs(message));
		}

		public VantageKind GetVantage(DiceRollType type, Ability savingThrow, Skills skillCheck = Skills.none, VantageKind initialVantage = VantageKind.Normal)
		{
			int advantageCount = 0;
			int disadvantageCount = 0;
			switch (initialVantage)
			{
				case VantageKind.Advantage:
					advantageCount = 1;
					break;
				case VantageKind.Disadvantage:
					disadvantageCount = 1;
					break;
			}

			foreach (string key in VantageMods.Keys)
			{
				if (VantageMods[key].RollType == type)
				{
					switch (type)
					{
						case DiceRollType.SkillCheck:
							Skills matchSkill = VantageMods[key].Detail;
							if ((DndUtils.IsSkillAGenericAbility(matchSkill) && matchSkill == DndUtils.FromSkillToAbility(skillCheck)) ||
								matchSkill == skillCheck)
							{
								if (VantageMods[key].Offset < 0)
								{
									OnRequestMessageToDungeonMaster($"{Name} has disadvantage on {matchSkill} skill checks due to {VantageMods[key].DieLabel}.");
									disadvantageCount++;
								}
								else if (VantageMods[key].Offset > 0)
								{
									OnRequestMessageToDungeonMaster($"{Name} has advantage on {matchSkill} skill checks due to {VantageMods[key].DieLabel}.");
									advantageCount++;
								}
							}
							break;
						// TODO: Implement vantage mods for attacks, initiative, etc.
						case DiceRollType.Attack:
						case DiceRollType.ChaosBolt:

							break;
						case DiceRollType.SavingThrow:
						case DiceRollType.DamagePlusSavingThrow:
							if ((int)VantageMods[key].Detail == (int)savingThrow)
							{
								if (VantageMods[key].Offset < 0)
								{
									OnRequestMessageToDungeonMaster($"{Name} has disadvantage on {savingThrow} saving throws due to {VantageMods[key].DieLabel}.");
									disadvantageCount++;
								}
								else if (VantageMods[key].Offset > 0)
								{
									OnRequestMessageToDungeonMaster($"{Name} has advantage on {savingThrow} saving throws due to {VantageMods[key].DieLabel}.");
									advantageCount++;
								}
							}
							break;
						case DiceRollType.DeathSavingThrow:

							break;
						case DiceRollType.Initiative:

							break;
						case DiceRollType.NonCombatInitiative:

							break;
					}
				}
			}
			advantageCount = (int)MathUtils.Clamp(advantageCount, 0, 1);
			disadvantageCount = (int)MathUtils.Clamp(disadvantageCount, 0, 1);
			if (advantageCount == disadvantageCount)
				return VantageKind.Normal;
			else if (advantageCount > 0)
				return VantageKind.Advantage;
			else if (disadvantageCount > 0)
				return VantageKind.Disadvantage;

			return VantageKind.Normal;
		}

		public void RemoveVantageMod(string id)
		{
			if (!VantageMods.ContainsKey(id))
				return;
			VantageMods.Remove(id);
		}

		public virtual void ChangeTempHP(double deltaTempHp)
		{
			tempHitPoints += deltaTempHp;
			if (tempHitPoints < 0)
				tempHitPoints = 0;
		}

		public void ChangeHealth(double damageHealthAmount)
		{
			if (damageHealthAmount < 0)
				TakeDamage(DamageType.None, AttackKind.Any, -damageHealthAmount);
			else
				Heal(damageHealthAmount);
		}

		public void Heal(double deltaHealth)
		{
			if (deltaHealth <= 0)
				return;

			if (HitPoints >= maxHitPoints)
				return;

			double maxToHeal = maxHitPoints - HitPoints;
			if (deltaHealth > maxToHeal)
				HitPoints = maxHitPoints;
			else
				HitPoints += deltaHealth;
		}

		public abstract double GetSavingThrowModifier(Ability savingThrowAbility);

		void GetDamageAndAttackType(string damageStr, out DamageType damageType, out AttackKind attackKind)
		{
			attackKind = AttackKind.Any;
			if (damageStr.ToLower().StartsWith("and "))
				damageStr = damageStr.Substring(4);
			int spaceIndex = damageStr.IndexOf(' ');
			if (spaceIndex > 0)
			{
				string remainingStr = damageStr.Substring(spaceIndex).Trim();
				if (remainingStr.ToLower().Contains("nonmagical") || remainingStr.ToLower().Contains("non-magical"))
					attackKind = AttackKind.NonMagical;
				else if (remainingStr.ToLower().Contains("magical"))
					attackKind = AttackKind.Magical;
				damageStr = damageStr.Substring(0, spaceIndex);
			}
			damageType = DndUtils.ToDamage(damageStr);
		}

		public void SetDamageImmunities(string damageImmunities)
		{
			if (string.IsNullOrWhiteSpace(damageImmunities))
				return;

			damageImmunities = damageImmunities.Replace(';', ',');
			string[] parts = damageImmunities.Split(',');
			foreach (string part in parts)
			{
				GetDamageAndAttackType(part.Trim(), out DamageType damage, out AttackKind attackKind);
				if (damage != DamageType.None)
					AddDamageImmunity(damage, attackKind);
			}
		}

		public void SetDamageResistances(string damageResistances)
		{
			if (string.IsNullOrWhiteSpace(damageResistances))
				return;

			damageResistances = damageResistances.Replace(';', ',');
			string[] parts = damageResistances.Split(',');
			foreach (string part in parts)
			{
				GetDamageAndAttackType(part.Trim(), out DamageType damage, out AttackKind attackKind);
				if (damage != DamageType.None)
					AddDamageResistance(damage, attackKind);
			}
		}

		public void SetConditionImmunities(string conditionImmunities)
		{
			//if (!string.IsNullOrWhiteSpace(conditionImmunities))
			//{
			//	System.Diagnostics.Debugger.Break();
			//}
		}

		public void SetDamageVulnerabilities(string damageVulnerabilities)
		{
			if (string.IsNullOrWhiteSpace(damageVulnerabilities))
				return;

			damageVulnerabilities = damageVulnerabilities.Replace(';', ',');
			string[] parts = damageVulnerabilities.Split(',');
			foreach (string part in parts)
			{
				GetDamageAndAttackType(part.Trim(), out DamageType damage, out AttackKind attackKind);
				if (damage != DamageType.None)
					AddDamageVulnerability(damage, attackKind);
			}
		}
	}
}