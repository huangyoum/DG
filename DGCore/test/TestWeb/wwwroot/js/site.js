﻿var Class = (function () {
    var _mix = function (r, s) {
        for (var p in s) {
            if (s.hasOwnProperty(p)) {
                r[p] = s[p]
            }
        }
    }

    var _extend = function () {

        //开关 用来使生成原型时,不调用真正的构成流程init
        this.initPrototype = true
        var prototype = new this()
        this.initPrototype = false

        var items = Array.prototype.slice.call(arguments) || []
        var item

        //支持混入多个属性，并且支持{}也支持 Function
        while (item = items.shift()) {
            _mix(prototype, item.prototype || item)
        }


        // 这边是返回的类，其实就是我们返回的子类
        function SubClass() {
            if (!SubClass.initPrototype && this.init)
                this.init.apply(this, arguments)//调用init真正的构造函数
        }

        // 赋值原型链，完成继承
        SubClass.prototype = prototype

        // 改变constructor引用
        SubClass.prototype.constructor = SubClass

        // 为子类也添加extend方法
        SubClass.extend = _extend

        return SubClass
    }
    //超级父类
    var Class = function () { }
    //为超级父类添加extend方法
    Class.extend = _extend

    return Class
})()

//辅组函数，获取数组里某个元素的索引 index
var _indexOf = function (array, key) {
    if (array === null) return -1
    var i = 0, length = array.length
    for (; i < length; i++) if (array[i] === item) return i
    return -1
}

var Event = Class.extend({
    //添加监听
    on: function (key, listener) {
        //this.__events存储所有的处理函数
        if (!this.__events) {
            this.__events = {}
        }
        if (!this.__events[key]) {
            this.__events[key] = []
        }
        if (_indexOf(this.__events, listener) === -1 && typeof listener === 'function') {
            this.__events[key].push(listener)
        }

        return this
    },
    //触发一个事件，也就是通知
    fire: function (key) {

        if (!this.__events || !this.__events[key]) return

        var args = Array.prototype.slice.call(arguments, 1) || []

        var listeners = this.__events[key]
        var i = 0
        var l = listeners.length

        for (i; i < l; i++) {
            listeners[i].apply(this, args)
        }

        return this
    },
    //取消监听
    off: function (key, listener) {

        if (!key && !listener) {
            this.__events = {}
        }
        //不传监听函数，就去掉当前key下面的所有的监听函数
        if (key && !listener) {
            delete this.__events[key]
        }

        if (key && listener) {
            var listeners = this.__events[key]
            var index = _indexOf(listeners, listener)

                (index > -1) && listeners.splice(index, 1)
        }

        return this;
    }
})

var Base = Class.extend(Event, {
    init: function (config) {
        //自动保存配置项
        this.__config = config
        this.bind()
        this.render()
    },
    //可以使用get来获取配置项
    get: function (key) {
        return this.__config[key]
    },
    //可以使用set来设置配置项
    set: function (key, value) {
        this.__config[key] = value
    },
    bind: function () {
    },
    render: function () {

    },
    //定义销毁的方法，一些收尾工作都应该在这里
    destroy: function () {
        //去掉所有的事件监听
        this.off()
    }
})


var TextCount = Base.extend({
    _getNum: function () {
        return this.get('input').val().length;
    },
    bind: function () {
        var self = this;
        self.get('input').on('keyup', function () {
            //通知,每当有输入的时候，就报告出去。
            self.fire('Text.input', self._getNum())
            self.render();
        });
    },
    render: function () {
        var num = this._getNum();
        if ($('#J_input_count').length == 0) {
            this.get('input').after('<span id="J_input_count"></span>');
        };

        $('#J_input_count').html(num + '个字');

    }
})

$(function () {
    var t = new TextCount({
        input: $("#J_input")
    });
    //监听这个输入事件
    t.on('Text.input', function (num) {
        //可以获取到传递过来的值
        if (num > 5) {
            alert(this.value)
        }
    })

})
