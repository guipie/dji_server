<template>
	<div class="chatModel-container">
		<el-dialog v-model="isShowDialog" :width="800" draggable="">
			<template #header>
				<div style="color: #fff">
					<!--<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>-->
					<span>{{ props.title }}</span>
				</div>
			</template>
			<el-form :model="ruleForm" ref="ruleFormRef" label-width="auto" :rules="rules">
				<el-row :gutter="35">
					<el-form-item v-show="false">
						<el-input v-model="ruleForm.id" />
					</el-form-item>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="模型" prop="modelId">
							<el-input v-model="ruleForm.modelId" placeholder="请输入模型" maxlength="200" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="模型" prop="name">
							<el-input v-model="ruleForm.name" placeholder="请输入模型" maxlength="200" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="模型短称" prop="shortName">
							<el-input v-model="ruleForm.shortName" placeholder="请输入模型短称" maxlength="200" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="模型头像" prop="avatarUrl">
							<el-input v-model="ruleForm.avatarUrl" placeholder="请输入模型头像" maxlength="255" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="模型类型" prop="modelType">
							<el-select clearable v-model="ruleForm.modelType" placeholder="请选择模型类型">
								<el-option v-for="(item,index) in dl('code_gen_net_type')"  :key="index" :value="item.code" :label="`[${item.code}] ${item.value}`"></el-option>
								
							</el-select>
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="模型种类供应商" prop="category">
							<el-input v-model="ruleForm.category" placeholder="请输入模型种类供应商" maxlength="255" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="接口地址" prop="url">
							<el-input v-model="ruleForm.url" placeholder="请输入接口地址" maxlength="200" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="最大token数量" prop="maxToken">
							<el-input-number v-model="ruleForm.maxToken" placeholder="请输入最大token数量" clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="模型描述" prop="desc">
							<el-input v-model="ruleForm.desc" placeholder="请输入模型描述" maxlength="2000" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="千个token多少钱" prop="thousandTokenCoin">
							<el-input-number v-model="ruleForm.thousandTokenCoin" placeholder="请输入千个token多少钱" clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="模型标签逗号分割" prop="tags">
							<el-input v-model="ruleForm.tags" placeholder="请输入模型标签逗号分割" type="textarea" maxlength="255" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item label="模型设置" prop="settings">
							<el-input v-model="ruleForm.settings" placeholder="请输入模型设置" maxlength="4000" show-word-limit clearable />
							
						</el-form-item>
						
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">取 消</el-button>
					<el-button type="primary" @click="submit">确 定</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>
<style scoped>
:deep(.el-select),
:deep(.el-input-number) {
	width: 100%;
}
</style>
<script lang="ts" setup>
	import { ref,onMounted } from "vue";
	import { getDictDataItem as di, getDictDataList as dl } from '/@/utils/dict-utils';
	import { ElMessage } from "element-plus";
	import type { FormRules } from "element-plus";
	import { addChatModel, updateChatModel, detailChatModel } from "/@/api/main/chatModel";

	//父级传递来的参数
	var props = defineProps({
		title: {
		type: String,
		default: "",
	},
	});
	//父级传递来的函数，用于回调
	const emit = defineEmits(["reloadTable"]);
	const ruleFormRef = ref();
	const isShowDialog = ref(false);
	const ruleForm = ref<any>({});
	//自行添加其他规则
	const rules = ref<FormRules>({
		modelId: [{required: true, message: '请输入模型！', trigger: 'blur',},],
		name: [{required: true, message: '请输入模型！', trigger: 'blur',},],
		shortName: [{required: true, message: '请输入模型短称！', trigger: 'blur',},],
		modelType: [{required: true, message: '请选择模型类型！', trigger: 'change',},],
		category: [{required: true, message: '请输入模型种类供应商！', trigger: 'blur',},],
		maxToken: [{required: true, message: '请输入最大token数量！', trigger: 'blur',},],
		thousandTokenCoin: [{required: true, message: '请输入千个token多少钱！', trigger: 'blur',},],
		tags: [{required: true, message: '请输入模型标签逗号分割！', trigger: 'blur',},],
	});

	// 打开弹窗
	const openDialog = async (row: any) => {
		// ruleForm.value = JSON.parse(JSON.stringify(row));
		// 改用detail获取最新数据来编辑
		let rowData = JSON.parse(JSON.stringify(row));
		if (rowData.id)
			ruleForm.value = (await detailChatModel(rowData.id)).data.result;
		else
			ruleForm.value = rowData;
		isShowDialog.value = true;
	};

	// 关闭弹窗
	const closeDialog = () => {
		emit("reloadTable");
		isShowDialog.value = false;
	};

	// 取消
	const cancel = () => {
		isShowDialog.value = false;
	};

	// 提交
	const submit = async () => {
		ruleFormRef.value.validate(async (isValid: boolean, fields?: any) => {
			if (isValid) {
				let values = ruleForm.value;
				if (ruleForm.value.id == undefined || ruleForm.value.id == null || ruleForm.value.id == "" || ruleForm.value.id == 0) {
					await addChatModel(values);
				} else {
					await updateChatModel(values);
				}
				closeDialog();
			} else {
				ElMessage({
					message: `表单有${Object.keys(fields).length}处验证失败，请修改后再提交`,
					type: "error",
				});
			}
		});
	};







	// 页面加载时
	onMounted(async () => {
	});

	//将属性或者函数暴露给父组件
	defineExpose({ openDialog });
</script>




