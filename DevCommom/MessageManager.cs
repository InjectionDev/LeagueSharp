using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DevCommom
{
    public class MessageManager
    {

        public bool IsActive;
        private List<MessageDTO> messageDTOList;

        public MessageManager()
        {
            //this.messageDTOList = new List<MessageDTO>();
            //this.IsActive = true;

            //Drawing.OnDraw += Drawing_OnDraw;
        }

        public void AddMessage(int pSlot, string pMessage, System.Drawing.Color pColor)
        {
            var query = this.messageDTOList.Where(x => x.Slot == pSlot);
            if (query.Any())
            {
                var message = query.First();
                message.Message = pMessage;
                message.Color = pColor;
            }
            else
            {
                this.messageDTOList.Add(new MessageDTO() { Slot = pSlot, Message = pMessage, Color = pColor });
            }
        }


        public void Draw()
        {
            if (this.IsActive)
            {
                foreach (var messageDTO in this.messageDTOList)
                {
                    Drawing.DrawText(messageDTO.GetPosX, messageDTO.GetPosY, messageDTO.Color, messageDTO.Message);
                }
            }
        }

        void Drawing_OnDraw(EventArgs args)
        {
            if (this.IsActive)
            {
                foreach (var messageDTO in this.messageDTOList)
                {
                    Drawing.DrawText(messageDTO.GetPosX, messageDTO.GetPosY, messageDTO.Color, messageDTO.Message);
                }
            }
        }


        public struct MessageDTO {
            public int Slot { get; set; }
            public string Message { get; set; }
            public System.Drawing.Color Color { get; set; }

            public float GetPosX { get { return Drawing.Width * 0.90f; } }

            public float GetPosY { get { return Drawing.Height * 0.68f + (Slot * 30); } }
        }

    }
}
