using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DevCommom
{
    public class ItemManager
    {
        private List<ItemDTO> ItemDTOList;


        public ItemManager()
        {
            this.ItemDTOList = new List<ItemDTO>();
            this.InitiliazeItemList();
        }

        public bool IsItemReady(ItemName pItemName)
        {
            return this.ItemDTOList.Where(x => x.ItemName == pItemName).First().Item.IsReady();
        }

        public void UseItem(ItemName pItemName, Obj_AI_Hero target = null)
        {
            var item = this.ItemDTOList.Where(x => x.ItemName == pItemName).First().Item;

            if (!item.IsReady())
                return;

            if (target == null)
                item.Cast();
            else
                item.Cast(target);
        }

        private void InitiliazeItemList()
        {
            this.ItemDTOList.Add(new ItemDTO
            {
                Item = new Items.Item(3144, 450),
                ItemName = ItemName.BilgewaterCutlass
            });
            this.ItemDTOList.Add(new ItemDTO
            {
                Item = new Items.Item(3188, 750),
                ItemName = ItemName.BlackfireTorch
            });
            this.ItemDTOList.Add(new ItemDTO
            {
                Item = new Items.Item(3153, 450),
                ItemName = ItemName.BladeOfTheRuineKing
            });
            this.ItemDTOList.Add(new ItemDTO
            {
                Item = new Items.Item(3128, 750),
                ItemName = ItemName.DeathfireGrasp
            });
            this.ItemDTOList.Add(new ItemDTO
            {
                Item = new Items.Item(3146, 700),
                ItemName = ItemName.HextechGunblade
            });
            this.ItemDTOList.Add(new ItemDTO
            {
                Item = new Items.Item(3042, int.MaxValue),
                ItemName = ItemName.Muramana
            });
            this.ItemDTOList.Add(new ItemDTO
            {
                Item = new Items.Item(3074, 400),
                ItemName = ItemName.RavenousHydra
            });
            this.ItemDTOList.Add(new ItemDTO
            {
                Item = new Items.Item(3077, 400),
                ItemName = ItemName.Tiamat
            });
            this.ItemDTOList.Add(new ItemDTO
            {
                Item = new Items.Item(3142, (int)(ObjectManager.Player.AttackRange * 2)),
                ItemName = ItemName.YoumuusGhostblade
            });
        }

    }

    public class ItemDTO
    {
        public Items.Item Item { get; set; }
        public ItemName ItemName { get; set; }
    }

    public enum ItemName
    {
        BilgewaterCutlass,
        BlackfireTorch,
        BladeOfTheRuineKing,
        DeathfireGrasp,
        HextechGunblade,
        Muramana,
        RavenousHydra,
        Tiamat,
        YoumuusGhostblade,

    }


}
