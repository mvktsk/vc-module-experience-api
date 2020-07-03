using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ShippingModule.Core.Model;

namespace VirtoCommerce.XPurchase.Extensions
{
    public static class RewardExtensions
    {
        public static void ApplyRewards(this PaymentMethod paymentMethod, ICollection<PromotionReward> rewards)
            => paymentMethod.DiscountAmount = rewards
                .Where(r => r.IsValid)
                .OfType<PaymentReward>()
                .Where(r => r.PaymentMethod.IsNullOrEmpty() || r.PaymentMethod.EqualsInvariant(paymentMethod.Code))
                .Sum(reward => reward.GetRewardAmount(paymentMethod.Price - paymentMethod.DiscountAmount, 1));

        public static void ApplyRewards(this ShippingRate shippingRate, ICollection<PromotionReward> rewards)
            => shippingRate.DiscountAmount = rewards
                .Where(r => r.IsValid)
                .OfType<ShipmentReward>()
                .Where(r => r.ShippingMethod.IsNullOrEmpty() || (shippingRate.ShippingMethod != null && r.ShippingMethod.EqualsInvariant(shippingRate.ShippingMethod.Code)))
                .Sum(reward => reward.GetRewardAmount(shippingRate.Rate, 1));

        public static void ApplyRewards(this ShoppingCart shoppingCart, ICollection<PromotionReward> rewards)
        {
            shoppingCart.Discounts?.Clear();
            shoppingCart.DiscountAmount = 0M;

            var cartRewards = rewards.OfType<CartSubtotalReward>();
            foreach (var reward in cartRewards.Where(reward => reward.IsValid))
            {
                //When a discount is applied to the cart subtotal, the tax calculation has already been applied, and is reflected in the tax subtotal.
                //Therefore, a discount applying to the cart subtotal will occur after tax.
                //For instance, if the cart subtotal is $100, and $15 is the tax subtotal, a cart - wide discount of 10 % will yield a total of $105($100 subtotal – $10 discount + $15 tax on the original $100).
                var discount = new Discount
                {
                    Coupon = reward.Coupon,
                    Currency = shoppingCart.Currency,
                    Description = reward.Promotion?.Description,
                    DiscountAmount = reward.GetRewardAmount(shoppingCart.SubTotal, 1),
                    PromotionId = reward.PromotionId,
                };
                if (shoppingCart.Discounts == null)
                {
                    shoppingCart.Discounts = new List<Discount>();
                }
                shoppingCart.Discounts.Add(discount);
                shoppingCart.DiscountAmount += discount.DiscountAmount;
            }

            var lineItemRewards = rewards.OfType<CatalogItemAmountReward>();
            foreach (var lineItem in shoppingCart.Items)
            {
                lineItem.ApplyRewards(shoppingCart.Currency, lineItemRewards);
            }

            var shipmentRewards = rewards.OfType<ShipmentReward>();
            foreach (var shipment in shoppingCart.Shipments)
            {
                shipment.ApplyRewards(shoppingCart.Currency, shipmentRewards);
            }

            var paymentRewards = rewards.OfType<PaymentReward>();
            foreach (var payment in shoppingCart.Payments)
            {
                payment.ApplyRewards(shoppingCart.Currency, paymentRewards);
            }
        }

        public static void ApplyRewards(this LineItem lineItem, string currency, IEnumerable<CatalogItemAmountReward> rewards)
        {
            var lineItemRewards = rewards
                .Where(r => r.IsValid)
                .Where(r => r.ProductId.IsNullOrEmpty() || r.ProductId.EqualsInvariant(lineItem.ProductId));

            
            lineItem.Discounts?.Clear();
            lineItem.DiscountAmount = Math.Max(0, lineItem.ListPrice - lineItem.SalePrice);

            if (lineItem.Quantity == 0)
            {
                return;
            }

            foreach (var reward in lineItemRewards)
            {
                var discount = new Discount
                {
                    Coupon = reward.Coupon,
                    Currency = currency,
                    Description = reward.Promotion?.Description,
                    DiscountAmount = reward.GetRewardAmount(lineItem.ListPrice - lineItem.DiscountAmount, lineItem.Quantity),
                    PromotionId = reward.PromotionId ?? reward.Promotion?.Id,
                };

                // Pass invalid discounts
                if (discount.DiscountAmount <= 0)
                {
                    continue;
                }

                if (lineItem.Discounts == null)
                {
                    lineItem.Discounts = new List<Discount>();
                }
                lineItem.Discounts.Add(discount);
                lineItem.DiscountAmount += discount.DiscountAmount;
            }
        }

        public static void ApplyRewards(this Shipment shipment, string currency, IEnumerable<ShipmentReward> rewards)
        {
            var shipmentRewards = rewards
                .Where(r => r.IsValid)
                .Where(r => r.ShippingMethod.IsNullOrEmpty() || r.ShippingMethod.EqualsInvariant(shipment.ShipmentMethodCode));

            shipment.Discounts?.Clear();
            shipment.DiscountAmount = 0M;

            foreach (var reward in shipmentRewards)
            {
                var discount = new Discount
                {
                    Coupon = reward.Coupon,
                    Currency = currency,
                    Description = reward.Promotion?.Description,
                    DiscountAmount = reward.GetRewardAmount(shipment.Price - shipment.DiscountAmount, 1),
                    PromotionId = reward.PromotionId ?? reward.Promotion?.Id,
                };

                // Pass invalid discounts
                if (discount.DiscountAmount <= 0)
                {
                    continue;
                }
                if (shipment.Discounts == null)
                {
                    shipment.Discounts = new List<Discount>();
                }
                shipment.Discounts.Add(discount);
                shipment.DiscountAmount += discount.DiscountAmount;
            }
        }

        public static void ApplyRewards(this Payment payment, string currency, IEnumerable<PaymentReward> rewards)
        {
            var paymentRewards = rewards
                .Where(r => r.IsValid)
                .Where(r => r.PaymentMethod.IsNullOrEmpty() || r.PaymentMethod.EqualsInvariant(payment.PaymentGatewayCode));

            payment.Discounts?.Clear();
            payment.DiscountAmount = 0M;

            foreach (var reward in paymentRewards)
            {
                var discount = new Discount
                {
                    Coupon = reward.Coupon,
                    Currency = currency,
                    Description = reward.Promotion?.Description,
                    DiscountAmount = reward.GetRewardAmount(payment.Price - payment.DiscountAmount, 1),
                    PromotionId = reward.PromotionId ?? reward.Promotion?.Id,
                };

                // Pass invalid discounts
                if (discount.DiscountAmount <= 0)
                {
                    continue;
                }
                if (payment.Discounts == null)
                {
                    payment.Discounts = new List<Discount>();
                }
                payment.Discounts.Add(discount);
                payment.DiscountAmount += discount.DiscountAmount;
            }
        }
    }
}